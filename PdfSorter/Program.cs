using Amazon.S3;
using Amazon.S3.Model;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using PdfSorter;
using PdfSorter.AWS;
using PdfSorter.Data;
using PdfSorter.Exceptions;
using Serilog;
using System.IO.Compression;

Setup setup;
ProcessingMetadataContext context;
AWSMethods awsMethods;
string path;

// Try to initialize (get configurations, aws methods, local folders created).
try
{
    Log.Debug("Initializing...");
    setup = new Setup();
    awsMethods = setup.AwsMethods;
    path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $"/PdfSorterData/";
    // Does not need to check for existence first.
    DirectoryInfo dir = Directory.CreateDirectory(path);
    foreach (FileInfo file in dir.EnumerateFiles())
    {
        file.Delete();
    }
    context = new(path, setup.DatabaseFileName);
}
catch (Exception ex)
{
    Log.Error($"Application failed to initialize properly: {ex.Message}");
    throw new PdfSorterSetupFailedException(ex.Message, ex);
}

// Try to download the most recent DB file, if none exists create a new one.
try
{
    await awsMethods.DownloadObjectFromBucketAsync(setup.DatabaseFileName, path);
}
catch (AmazonS3Exception ex)
{
    if (ex.ErrorCode == "NoSuchKey")
    {
        Log.Debug("Database did not exist on S3 bucket, creating local database.");
        await context.Database.EnsureCreatedAsync();
    }
    else
    {
        throw new PdfSorterSetupFailedException(ex.Message, ex);
    }
}

// Create a new event for this run.
ProcessEvent processEvent = new();
context.ProcessEvents.Add(processEvent);
await context.SaveChangesAsync();

// These variables contains previously processed zips/files.
List<ProcessedZip> alreadyProcessedZips = await context.ProcessedZips.ToListAsync();
List<string> alreadyProcessedFiles = await context.ProcessedFiles.Select(p => p.FileName).ToListAsync();

// Get the list of existing objects.
List<S3Object>? filesInBucket = await awsMethods.GetListOfAWSObjectsAsync();

// If there were no files in the bucket no need to continue.
if (filesInBucket != null && filesInBucket.Any())
{
    // This would be fairly significantly improved if we had a guaranteed
    // prefix for the .zip files to pass to ListObjectsV2Async
    filesInBucket = filesInBucket.Where(f => f.Key.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();
    Log.Debug("Seeing if we need to download files from AWS...");
    foreach (S3Object obj in filesInBucket)
    {
        // Only need to download it locally we've never seen it or
        // if it has updated since last time we processed it.
        // ASSUMPTION: the zip could be updated after we process it once.
        ProcessedZip? previouslyProcessedZip = alreadyProcessedZips.Where(a => a.FileName == obj.Key).FirstOrDefault();
        if (previouslyProcessedZip == null || previouslyProcessedZip.LastUpdateDateTime <= obj.LastModified)
        {
            try
            {
                Log.Debug($"Downloading {obj.Key}");
                await awsMethods.DownloadObjectFromBucketAsync(obj.Key, path);
            }
            catch
            {
                Log.Error($"Failed to download {obj.Key} from AWS.");
                // Attempt to delete the file in case it was half-way created/corrupted while failing.
                File.Delete(path + "/" + obj.Key);
            }
        }
    }

    // Get a list of files we actually downloaded.
    List<string>? downloadedZipPaths = Directory.GetFiles(path).Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();

    if (downloadedZipPaths != null && downloadedZipPaths.Any())
    { 
        foreach (string zipPath in downloadedZipPaths)
        {
            try 
            {
                ProcessedZip processedZip = await CreateOrUpdateZipFile(zipPath, alreadyProcessedZips, processEvent, context);

                // Within each Zip file.
                using ZipArchive archive = ZipFile.OpenRead(zipPath);
                // Find the mapping CSV file.
                // TODO: Edge case, if there is more than 1 CSV we will not capture that.
                ZipArchiveEntry? csvFile = archive.Entries.Where(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

                if (csvFile != null)
                {
                    Dictionary<string, List<string>> fileMatchToPo = GetMappingsFromCsv(csvFile, setup.CsvConfig);

                    Log.Debug($"{fileMatchToPo.Count} PO Numbers being tracked for file {csvFile.FullName}");

                    // For each pdf file in the zip file.
                    foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
                    {
                        // If we've already processed the file, don't do it again.
                        if (!alreadyProcessedFiles.Contains(entry.FullName))
                        {
                            try
                            {
                                string poNum = fileMatchToPo.Where(f => f.Value.Contains(entry.FullName)).FirstOrDefault().Key;
                                if (poNum != string.Empty)
                                {
                                    var pathToSave = $"{path}/by-po/{poNum}/";
                                    Directory.CreateDirectory(pathToSave);

                                    ProcessedFile processedFile = new()
                                    {
                                        FileName = entry.FullName,
                                        ProcessedZipId = processedZip.Id,
                                        PONumber = poNum
                                    };

                                    processedZip.LastUpdateDateTime = DateTime.UtcNow;

                                    entry.ExtractToFile(pathToSave + entry.FullName);

                                    await awsMethods.UploadFileAsync(pathToSave + entry.FullName, $"by-po/{poNum}/{entry.FullName}");

                                    // TODO: If we fail to write metadata should we "rollback" the file move?
                                    try
                                    {
                                        context.ProcessedFiles.Add(processedFile);
                                        context.ProcessedZips.Update(processedZip);
                                        await context.SaveChangesAsync();
                                        // Don't forget to add it to our list of existing entries we're tracking!
                                        alreadyProcessedFiles.Add(entry.FullName);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Error(ex.ToString());
                                    }
                                }
                                // TODO: What to do with these?
                                else
                                {
                                    Log.Warning($"No PO found for {entry.FullName} in {zipPath}.");
                                }
                            }
                            catch (IOException ex)
                            {
                                Log.Error($"Could not write {entry.FullName}. Exception: {ex.Message}");
                            }
                        }
                    }
                }
                else
                {
                    Log.Error($"There is no .csv file within file: {processedZip.FileName}");
                }
            }
            // Catching this will prevent us from bombing out mid process for one file.
            catch (Exception ex)
            {
                Log.Error($"Something went wrong processing {zipPath}", ex);
            }
    }
}
}

// Cleanup.
processEvent.CompleteTime = DateTime.UtcNow;
context.ProcessEvents.Update(processEvent);
await context.SaveChangesAsync();
await context.DisposeAsync();


await awsMethods.UploadFileAsync(path + setup.DatabaseFileName, setup.DatabaseFileName);


foreach (var file in new DirectoryInfo(path).EnumerateFiles())
{
    file.Delete();
}

Log.Information("Finished processing!");


/// <summary>
/// This method creates or updates a zip file in context.
/// </summary>
/// <param name="zipPath">The path to the zip file.</param>
/// <param name="processedZips">A list of zip files already processed.</param>
/// <param name="processEvent">The entity for the current event.</param>
/// <param name="context">The processing metadata database context.</param>
/// <returns>The created processed zip file.</returns>
static async Task<ProcessedZip> CreateOrUpdateZipFile(string zipPath, List<ProcessedZip> processedZips, ProcessEvent processEvent, ProcessingMetadataContext context)
{
    // If we've previously processed this zip, update it as being re-run.
    // Or it is the first time we've processed it, so create our entity
    // for tracking the zip file so we can associate pdfs with it.
    ProcessedZip? processedZip = processedZips.Where(a => a.FileName == zipPath[(zipPath.LastIndexOf('/') + 1)..]).FirstOrDefault();
    if (processedZip != null)
    {
        Log.Debug($"{processedZip.FileName} has been previously processed. Updating it.");
        processedZip.LastUpdateDateTime = DateTime.UtcNow;
        processedZip.LastUpdateProcessEventId = processEvent.Id;
    }
    else
    {
        processedZip = new()
        {
            FileName = zipPath[(zipPath.LastIndexOf('/') + 1)..],
            OriginalProcessEventId = processEvent.Id,
            LastUpdateProcessEventId = processEvent.Id
        };
        Log.Debug($"{processedZip.FileName} has not been previously processed. creating it.");
        context.ProcessedZips.Add(processedZip);
        await context.SaveChangesAsync();
    }
    return processedZip;
}

/// <summary>
/// This method creates a "map" of PO numbers to attachments.
/// </summary>
/// <param name="csvFile">A zip entry correlating to a CSV file.</param>
/// <param name="csvConfiguration">The configuration to use while reading the CSV.</param>
/// <returns>A dictionary of PO numbers that has attachment names associated.</returns>
static Dictionary<string, List<string>> GetMappingsFromCsv(ZipArchiveEntry csvFile, CsvConfiguration csvConfiguration)
{
    // Assumptions:
    // 1) Every .zip file is "self contained", in that the mapping csv record
    // for every file in a ZIP is in the same ZIP.
    // 2) There is exactly 1 csv per zip.
    Dictionary<string, List<string>> fileMatchToPo = new();
    using Stream stream = csvFile.Open();
    using StreamReader reader = new(stream);
    using CsvReader csv = new(reader, csvConfiguration);
    {
        IEnumerable<ExtractDataRaw> records = csv.GetRecords<ExtractDataRaw>();
        records = records.OrderBy(r => r.PONumber);
        foreach (ExtractDataRaw record in records)
        {
            // Attachments is comma delimited, even though the "csv" is ~ delimited.
            string[] attachments = record.AttachmentList.Split(',');


            if (!fileMatchToPo.ContainsKey(record.PONumber))
            {
                fileMatchToPo.Add(record.PONumber, new List<string>());
            }

            foreach (string attachmentPath in attachments)
            {
                var attachment = attachmentPath[(attachmentPath.LastIndexOf('/') + 1)..];
                // Add this file with the PO to our dict.
                fileMatchToPo[record.PONumber].Add(attachment);
            }
        }
    }
    return fileMatchToPo;
}