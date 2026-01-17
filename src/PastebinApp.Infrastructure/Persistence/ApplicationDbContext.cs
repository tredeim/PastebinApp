using Microsoft.EntityFrameworkCore;
using PastebinApp.Domain.Entities;
using PastebinApp.Infrastructure.Persistence.Configurations;

namespace PastebinApp.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Paste> Pastes => Set<Paste>();
    public DbSet<PasteHash> PasteHashes  => Set<PasteHash>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // modelBuilder.Entity<Paste>().HasData(...);
    }
}