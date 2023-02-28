# PDF Sorter
PDF Sorter is a C#/.NET Codebase that:
 - Maintains a history of past documents parsed using SQLite
 - Pulls down ZIP folders contained on an AWS S3 Bucket
 - Finds the mapping CSV file within the the ZIP folder
 - Maps any new PDF files to their associated PO directory back on the S3 Bucket

## Prerequisites
 - [.NET 7 SDK](https://dotnet.microsoft.com/en-us/download)

## Running locally
 - Override the values in appsettings.json using your 'User Secrets' file for the project to contain accurate AWSInfo.
    - Within Visual Studio on Windows, right click the project and hit 'Manage User Secrets'.
 - Navigate to the project folder (e.g./pdf-sorter/pdf-sorter)
 - Run the following command to build and run the app locally:

   ```bash
   dotnet run
   ```
## Usage


## Reference Materials
[AWS SDK S3 Examples](https://docs.aws.amazon.com/sdk-for-net/v3/developer-guide/csharp_s3_code_examples.html)  
[.NET Core Configuration](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration)  
[SQLite](https://www.sqlite.org/index.html)  
[User Secrets Guide](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows#secret-manager)  
