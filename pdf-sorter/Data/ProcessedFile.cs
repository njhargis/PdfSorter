using System.ComponentModel.DataAnnotations;

namespace pdf_sorter.Data
{
    public class ProcessedFile
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string FileName { get; set; }
        public required Guid ProcessedZipId { get; set; }
        public DateTime ProcessedDateTime { get; set; } = DateTime.UtcNow;
        public string? PONumber { get; set; }

        // This ensures if you have not initialized the parent due to data access strategy you cannot attempt to access said parent.
        // https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#required-navigation-properties
        private ProcessedZip? _processedZip;
        public ProcessedZip ProcessedZip 
        {  
            set => _processedZip = value;
            get => _processedZip ?? throw new InvalidOperationException("Uninitialized property: " + nameof(ProcessedZip));
        }

    }
}

