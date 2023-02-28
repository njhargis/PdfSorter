using Microsoft.EntityFrameworkCore;

namespace PdfSorter.Data
{
    public class ProcessingMetadataContext : DbContext
    {
        public DbSet<ProcessEvent> ProcessEvents { get; set; }
        public DbSet<ProcessedZip> ProcessedZips { get; set; }
        public DbSet<ProcessedFile> ProcessedFiles { get; set; }

        public string? DbPath { get; }

        public ProcessingMetadataContext(string path, string dbFileName)
        {
            DbPath = Path.Join(path, dbFileName);
        }

        // The following configures EF to connect to the Sqlite database file in the
        // passed in path from constructor.
        protected override void OnConfiguring(DbContextOptionsBuilder options)
        { 
            options.UseSqlite($"Data Source={DbPath};Pooling=False");

        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProcessedZip>()
                .HasOne(p => p.OriginalProcessEvent)
                .WithMany(pe => pe.OriginalProcessedZips)
                .HasForeignKey(p => p.OriginalProcessEventId);

            modelBuilder.Entity<ProcessedZip>()
                .HasOne(p => p.LastUpdateProcessEvent)
                .WithMany(pe => pe.UpdatedProcessedZips)
                .HasForeignKey(p => p.LastUpdateProcessEventId);
        }

    }
}
