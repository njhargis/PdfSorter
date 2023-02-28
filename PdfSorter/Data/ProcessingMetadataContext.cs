using Microsoft.EntityFrameworkCore;

namespace PdfSorter.Data
{
    public class ProcessingMetadataContext : DbContext
    {
        public DbSet<ProcessEvent> ProcessEvents { get; set; }
        public DbSet<ProcessedZip> ProcessedZips { get; set; }
        public DbSet<ProcessedFile> ProcessedFiles { get; set; }

        public string DbPath { get; }

        public ProcessingMetadataContext()
        {
            var folder = Environment.SpecialFolder.LocalApplicationData;
            var path = Environment.GetFolderPath(folder);
            DbPath = Path.Join(path, "ProcessingMetadata.db");
        }

        // The following configures EF to create a Sqlite database file in the
        // special "local" folder for your platform.
        // TODO: save this on aws
        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite($"Data Source={DbPath}");

    }
}
