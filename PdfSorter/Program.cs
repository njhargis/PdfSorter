using Amazon.S3;
using Amazon.S3.Model;
using CsvHelper;
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

try
{
    Log.Debug("Initializing...");
    setup = new Setup();
    awsMethods = setup.AwsMethods;
    path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + $"/PdfSorterData/";
    Directory.CreateDirectory(path);
    context = new(path, setup.DatabaseFileName);

    //TODO: Delete any old data, ensure path is created.
}
catch (Exception ex)
{
    Log.Error($"Application failed to initialize properly: {ex.Message}");
    throw new PdfSorterSetupFailedException(ex.Message, ex);
}

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

// This variable contains previously processed zips.
var alreadyProcessed = await context.ProcessedZips.ToArrayAsync();

// Get the list of existing objects.
List<S3Object>? filesInBucket = await awsMethods.GetListOfAWSObjectsAsync();

// If there were no files in the bucket no need to continue.
if (filesInBucket != null && filesInBucket.Any())
{
    foreach (S3Object obj in filesInBucket)
    {
        // TODO: This should be more DRY.
        // Only need to download it locally if it has updated since last time we processed it.
        // Assumption: the zip could be updated after we process it once.
        if (alreadyProcessed.Select(a => a.FileName).Contains(obj.Key))
        {
            if (alreadyProcessed.Where(a => a.FileName == obj.Key).FirstOrDefault()?.LastUpdateDateTime <= obj.LastModified)
            {
                try
                {
                    await awsMethods.DownloadObjectFromBucketAsync(obj.Key, path);
                }
                catch
                {
                    //TODO: Attempt to delete the file in case it was corrupted while failing.
                }
            }
        }
        else
        {
            try
            {
                await awsMethods.DownloadObjectFromBucketAsync(obj.Key, path);
            }
            catch
            {
                //TODO: Attempt to delete the file in case it was corrupted while failing.
            }
        }
    }


    List<string>? downloadedZipPaths = Directory.GetFiles(path).Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();

    if (downloadedZipPaths != null && downloadedZipPaths.Any())
    {
        foreach (string zipPath in downloadedZipPaths)
        {
            // If we've previously processed this zip, update it as being re-run.
            // Or it is the first time we've processed it, so create our entity
            // for tracking the zip file so we can associate pdfs with it.
            ProcessedZip? processedZip = alreadyProcessed.Where(a => a.FileName == zipPath[(zipPath.LastIndexOf('/') + 1)..]).FirstOrDefault();
            if (processedZip != null)
            {
                Log.Debug($"{processedZip.FileName} has been previously processed. Updating it.");
                processedZip.LastUpdateDateTime = DateTime.UtcNow;
                processedZip.LastUpdateProcessEventId = processEvent.Id;
            }
            else
            {
                Log.Debug($"{processedZip.FileName} has not been previously processed. creating it.");
                processedZip = new()
                {
                    FileName = zipPath[(zipPath.LastIndexOf('/') + 1)..],
                    OriginalProcessEventId = processEvent.Id,
                };
                context.ProcessedZips.Add(processedZip);
            }
            await context.SaveChangesAsync();

            // Within each Zip file.
            using ZipArchive archive = ZipFile.OpenRead(zipPath);

            // Find the mapping CSV file.
            ZipArchiveEntry csvFile = archive.Entries.Where(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

            // TODO: Catch if nothing gets filled into this?
            // Assumptions:
            // 1) Every .zip file is "self contained", in that the mapping csv record
            // for every file in a ZIP is in the same ZIP.
            // 2) There is exactly 1 csv per zip.
            Dictionary<string, List<string>> fileMatchToPo = new();
            using Stream stream = csvFile.Open();
            using StreamReader reader = new(stream);
            using CsvReader csv = new(reader, setup.CsvConfig);
            {
                // TODO: Handle if mapping fails for an individual file.
                // TODO: Handle if PO Number is null/empty?
                IEnumerable<ExtractDataRaw> records = csv.GetRecords<ExtractDataRaw>();
                records.OrderBy(r => r.PONumber);
                foreach (ExtractDataRaw record in records)
                {
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
            Log.Debug($"{fileMatchToPo.Count()} PO Numbers being tracked for file {csvFile.FullName}");

            // For each pdf file in the zip file.
            foreach (ZipArchiveEntry entry in archive.Entries.Where(e => e.FullName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    // What if this doesn't find a match?
                    string poNum = fileMatchToPo.Where(f => f.Value.Contains(entry.FullName)).FirstOrDefault().Key;
                    if (!Directory.Exists($"/data/by-po/" + poNum))
                    {
                        Directory.CreateDirectory($"/data/by-po/" + poNum);
                    }
                    if (poNum != string.Empty)
                    {
                        ProcessedFile processedFile = new()
                        {
                            FileName = entry.FullName,
                            ProcessedZipId = processedZip.Id,
                            PONumber = poNum
                        };

                        processedZip.LastUpdateDateTime = DateTime.UtcNow;

                        entry.ExtractToFile($"/data/by-po/" + poNum + "/" + entry.FullName);

                        // TODO: If we fail to write metadata should we "rollback" the file move?
                        try
                        {
                            context.ProcessedFiles.Add(processedFile);
                            context.ProcessedZips.Update(processedZip);
                            await context.SaveChangesAsync();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    // What to do with these?
                    else
                    {
                        Console.WriteLine($"No PO found for {entry.FullName} in {zipPath}.");
                    }
                }
                catch (IOException ex)
                {
                    Log.Error($"Could not write {entry.FullName}. Exception: {ex.Message}");
                }
            }
        }
    }
}

processEvent.CompleteTime = DateTime.UtcNow;
context.ProcessEvents.Update(processEvent);
await context.SaveChangesAsync();
