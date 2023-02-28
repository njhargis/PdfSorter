using Amazon;
using Amazon.S3;
using CsvHelper.Configuration;
using Microsoft.Extensions.Configuration;
using PdfSorter.AWS;
using PdfSorter.Data;
using Serilog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PdfSorter
{
    internal class Setup
    {
        public AWSInfo? AwsInfo { get; set; }
        public AWSMethods AwsMethods { get; set; }
        public CsvConfiguration CsvConfig { get; set; }
        public string DatabaseFileName { get; set; }
        public string LocalPath { get; set; }

        /// <summary>
        /// The constructor for this class instantiates and sets up various neccessary configurations.
        /// Pulling these configurations into a seperate class allows main to be focused on "flow" logic.
        /// </summary>
#pragma warning disable CS8601, CS8602, CS8618 // Possible null reference assignment.
        internal Setup()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .AddUserSecrets<Program>()
                .Build();

            // This local file is where we will temporarily store files and the database.
            LocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(config).CreateLogger();

            // Suppressing these null reference warnings, as GetRequiredSection will throw an exception if they are null.
            DatabaseFileName = config.GetRequiredSection("DatabaseFileName").Value;
            
            AwsInfo = config.GetRequiredSection("AWSInfo").Get<AWSInfo>();
            RegionEndpoint newRegion = RegionEndpoint.GetBySystemName(AwsInfo.Region);
            AmazonS3Client s3Client = new(AwsInfo.AccessKeyId, AwsInfo.Secret, newRegion);
            AwsMethods = new(s3Client, AwsInfo.BucketName);


            CsvConfig = new(CultureInfo.InvariantCulture)
            {
                Delimiter = "~",
                PrepareHeaderForMatch = args => Regex.Replace(args.Header.ToLower(), @"\s", string.Empty)
            };
#pragma warning restore CS8601, CS8602, CS8618 // Possible null reference assignment.
        }


    }
}
