using Microsoft.EntityFrameworkCore;

namespace EStore.Services.Database;

public class Context(DbContextOptions<Context> options) : DbContext(options)
{
    public DbSet<Product> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Product>()
            .HasIndex(p => p.StripePriceID)
            .IsUnique();

        modelBuilder.Entity<Product>()
            .HasIndex(p => p.StripeProductID)
            .IsUnique();
    }
}