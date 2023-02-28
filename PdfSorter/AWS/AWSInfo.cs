namespace PdfSorter.AWS
{
    internal class AWSInfo
    {
        public required string Region { get; set; }
        public required string BucketName { get; set; }
        public required string AccessKeyId { get; set; }
        public required string Secret { get; set; }
    }
}
