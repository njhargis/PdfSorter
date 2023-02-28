using Microsoft.EntityFrameworkCore;

namespace PdfSorter.Data
{
    public class ProcessEvent
    {
        public Guid Id { get; set; } = new Guid();
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? CompleteTime { get; set; }
        public ICollection<ProcessedZip> ProcessedZips { get; set; } = new HashSet<ProcessedZip>();
    }
}

