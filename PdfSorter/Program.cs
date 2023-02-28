using Amazon.S3.Model;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using PdfSorter;
using PdfSorter.Data;
using Serilog;
using System.IO.Compression;

var setup = new Setup();
var awsMethods = setup.AwsMethods;

//TODO: Init database if it doesn't exist, pull file down, connect to db, etc.
ProcessingMetadataContext context = new();
ProcessEvent processEvent = new();
context.ProcessEvents.Add(processEvent);
await context.SaveChangesAsync();

var alreadyProcessed = await context.ProcessedZips.ToArrayAsync();

// Get the list of existing objects.
List<S3Object>? filesInBucket = await awsMethods.GetListOfAWSObjectsAsync();

// If there were no files in the bucket or error accessing AWS server, no need to continue.
if (filesInBucket != null && filesInBucket.Any())
{
    foreach (S3Object obj in filesInBucket)
    {
        // TODO: Find out if we've already processed this file before we download.
        // TODO: If it fails while downloading then delete the corrupted object
        //await awsMethods.DownloadObjectFromBucketAsync(obj.Key, $"/data/");
    }

    List<string>? downloadedZipPaths = Directory.GetFiles($"/data/").Where(f => f.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)).ToList();

    if (downloadedZipPaths != null && downloadedZipPaths.Any())
    {
        foreach (string zipPath in downloadedZipPaths)
        {
            // If we've previously processed this zip, update it as being re-run.
            // Or it is the first time we've processed it, so create our entity
            // for tracking the zip file so we can associate pdfs with it.
            ProcessedZip processedZip;
            if (alreadyProcessed.Select(a => a.FileName).Contains(zipPath[(zipPath.LastIndexOf('/') + 1)..]))
            {
                processedZip = alreadyProcessed.Where(a => a.FileName == zipPath[(zipPath.LastIndexOf('/') + 1)..]).FirstOrDefault();
            }
            else
            {
                processedZip = new()
                {
                    FileName = zipPath[(zipPath.LastIndexOf('/') + 1)..],
                    ProcessEventId = processEvent.Id,
                };
                context.ProcessedZips.Add(processedZip);
            }

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
