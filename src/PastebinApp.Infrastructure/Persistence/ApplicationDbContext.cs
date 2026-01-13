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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new PasteConfiguration());
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        
    }
}