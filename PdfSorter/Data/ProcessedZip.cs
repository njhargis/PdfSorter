using System.ComponentModel.DataAnnotations;

namespace PdfSorter.Data
{
    public class ProcessedZip
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public required string FileName { get; set; }
        public required Guid OriginalProcessEventId { get; set; }
        public Guid LastUpdateProcessEventId { get; set; }
        public DateTime LastUpdateDateTime { get; set; } = DateTime.UtcNow;

        public ICollection<ProcessedFile> ProcessedFiles { get; set; } = new HashSet<ProcessedFile>();

        // This ensures if you have not initialized the parent due to data access strategy you cannot attempt to access said parent.
        // https://learn.microsoft.com/en-us/ef/core/miscellaneous/nullable-reference-types#required-navigation-properties
        private ProcessEvent? _originalProcessEvent;
        public ProcessEvent OriginalProcessEvent 
        {  
            set => _originalProcessEvent = value;
            get => _originalProcessEvent ?? throw new InvalidOperationException("Uninitialized property: " + nameof(OriginalProcessEvent));
        }

        private ProcessEvent? _lastUpdateProcessEvent;
        public ProcessEvent LastUpdateProcessEvent
        {
            set => _lastUpdateProcessEvent = value;
            get => _lastUpdateProcessEvent ?? throw new InvalidOperationException("Uninitialized property: " + nameof(LastUpdateProcessEvent));
        }

    }
}

