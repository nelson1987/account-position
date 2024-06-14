using Aeon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aeon.Infrastructure.Contexts;
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<Produto> Products { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        new ProductEntityTypeConfiguration().Configure(modelBuilder.Entity<Produto>());

        modelBuilder.Entity<Produto>().HasData(
            new Produto
            {
                Id = 1,
                FirstName = "System",
                LastName = "",
                isActive = true,
            }
        );
    }
}
