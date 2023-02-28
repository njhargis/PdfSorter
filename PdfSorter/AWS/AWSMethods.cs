using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Serilog;

namespace PdfSorter.AWS
{
    /// <summary>
    /// This class is convenient since we are only working with one bucket through one client.
    /// </summary>
    internal class AWSMethods
    {
        private readonly IAmazonS3 _client;
        private readonly string _bucketName;
        private readonly TransferUtility _transferUtility;

        /// <summary>
        /// The constructor for this class takes the client and bucket to use for all the methods.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="bucketName"></param>
        public AWSMethods(IAmazonS3 client, string bucketName)
        {
            _client = client;
            _bucketName = bucketName;
            _transferUtility = new TransferUtility(_client);
        }

        /// <summary>
        /// Gets all of the objects in an Amazon S3 bucket.
        /// </summary>
        /// <returns>A list of the S3Objects that were in the bucket.</returns>
        public async Task<List<S3Object>?> GetListOfAWSObjectsAsync()
        {
            try
            {
                List<S3Object> listObjects = new();
                var request = new ListObjectsV2Request
                {
                    BucketName = _bucketName
                };

                ListObjectsV2Response response;

                do
                {
                    response = await _client.ListObjectsV2Async(request);

                    response.S3Objects
                        .ForEach(listObjects.Add);

                    // If the response is truncated, set the request ContinuationToken
                    // from the NextContinuationToken property of the response.
                    request.ContinuationToken = response.NextContinuationToken;
                }
                while (response.IsTruncated);

                return listObjects;
            }
            // TODO: Handle 404 differently than other errors.
            catch (AmazonS3Exception ex)
            {
                Log.Error($"GetListOfAWSObjectsAsync: Error accessing bucket items: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Downloads an object from an Amazon S3 bucket to the
        /// local computer.
        /// </summary>
        /// <param name="objectName">The name of the object to download.</param>
        /// <param name="filePath">The path, including filename, where the
        /// downloaded object will be stored.</param>
        /// <returns>A boolean value indicating the success or failure of the
        /// download process.</returns>
        public async Task<bool> DownloadObjectFromBucketAsync(
                    string objectName,
                    string filePath)
        {
            // Create a GetObject request
            var request = new GetObjectRequest
            {
                BucketName = _bucketName,
                Key = objectName,
            };

            // Issue request and remember to dispose of the response
            using GetObjectResponse response = await _client.GetObjectAsync(request);

            try
            {
                // Save object to local file
                await response.WriteResponseStreamToFileAsync($"{filePath}\\{objectName}", true, CancellationToken.None);
                return response.HttpStatusCode == System.Net.HttpStatusCode.OK;
            }
            catch (AmazonS3Exception ex)
            {
                Log.Error($"DownloadObjectFromBucketAsync: Error downloading {objectName}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// This method checks if a file exists (or is accessible) on a S3 bucket using the current client.
        /// Optionally accept a versionId to check that specific version.
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="versionId"></param>
        /// <returns></returns>
        public async Task<bool> UploadFileAsync(string filePath, string keyName)
        {
            try
            {
                await _transferUtility.UploadAsync(filePath, _bucketName, keyName);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"UploadFileAsync: Error attempting to upload {keyName} to s3 bucket: {ex.Message}");
                return false;
            }
        }
    }
}
