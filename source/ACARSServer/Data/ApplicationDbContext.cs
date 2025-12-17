using ACARSServer.Model;
using Microsoft.EntityFrameworkCore;

namespace ACARSServer.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ApiKey>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VatsimCid).IsRequired().HasMaxLength(50);
            entity.Property(e => e.HashedKey).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.HashedKey).IsUnique();
        });
    }
}
