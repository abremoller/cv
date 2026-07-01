using Microsoft.EntityFrameworkCore;

namespace CV.Api.Data;

public class CvDbContext(DbContextOptions<CvDbContext> options) : DbContext(options)
{
    public DbSet<CvProfile> Profiles => Set<CvProfile>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Education> Education => Set<Education>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<CvProfile>(e =>
        {
            e.Property(p => p.Name).HasMaxLength(200);
            e.Property(p => p.Title).HasMaxLength(200);
            e.Property(p => p.Location).HasMaxLength(200);
            e.Property(p => p.Email).HasMaxLength(320);
        });

        b.Entity<Job>()
            .HasMany(j => j.Projects)
            .WithOne(p => p.Job!)
            .HasForeignKey(p => p.JobId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
