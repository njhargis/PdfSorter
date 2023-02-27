using CsvHelper.Configuration.Attributes;

namespace pdf_sorter.Data
{

    /// <summary>
    /// Everything comes in as a string to (try to) prevent conversions/poor data from breaking anything.
    /// As we just need a couple of fields at this point, no need to do conversions more elegantly, but if requirements
    /// change the fields already exist to handle more gracefully.
    /// </summary>
    public class ExtractDataRaw
    {
        public string Id { get; set; } = string.Empty;
        
        public string ClaimNumber { get; set; } = string.Empty;
        
        public string ClaimDate { get; set; } = string.Empty;

        public string OpenAmount { get; set; } = string.Empty;

        public string OriginalAmount { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string ArReasonCode { get; set; } = string.Empty;

        public string CustomerReasonCode { get; set; } = string.Empty;

        public string AttachmentList { get; set; } = string.Empty;

        public string CheckNumber { get; set; } = string.Empty;

        public string CheckDate { get; set; } = string.Empty;

        public string Comments { get; set; } = string.Empty;

        public string DaysOutstanding { get; set; } = string.Empty;

        public string Division { get; set; } = string.Empty;

        public string PONumber { get; set; } = string.Empty;

        public string Brand { get; set; } = string.Empty;

        public string MergeStatus { get; set; } = string.Empty;

        public string UnresolvedAmount { get; set; } = string.Empty;

        public string DocumentType { get; set; } = string.Empty;

        public string DocumentDate { get; set; } = string.Empty;

        public string OriginalCustomer { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public string CustomerLocation { get; set; } = string.Empty;

        public string CreateDate { get; set; } = string.Empty;

        public string LoadId { get; set; } = string.Empty;

        public string CarrierName { get; set; } = string.Empty;

        public string InvoiceStoreNumber { get; set; } = string.Empty;
    }
}
