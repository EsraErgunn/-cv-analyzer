using Microsoft.EntityFrameworkCore;

namespace ResultService.Data;

public class ResultDbContext : DbContext
{
    public ResultDbContext(DbContextOptions<ResultDbContext> options) : base(options)
    {
    }

    public DbSet<CvAnalysisResult> Results => Set<CvAnalysisResult>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CvAnalysisResult>(entity =>
        {
            entity.ToTable("cv_analysis_results");
            entity.HasKey(r => r.CvId);
            entity.HasIndex(r => r.UserId);
            entity.Property(r => r.FileName).HasMaxLength(512);
            entity.Property(r => r.AnalyzerName).HasMaxLength(64);
            // List<string> alanları Npgsql ile PostgreSQL text[] kolonlarına eşlenir.
        });
    }
}
