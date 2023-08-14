using Microsoft.EntityFrameworkCore;
using Products.Storage.Models;

namespace Products.Storage
{
    public class ProductsContext : DbContext
    {
        public DbSet<Product> Products { get; set; } = null!;

        public ProductsContext(DbContextOptions<ProductsContext> options)
            : base(options)
        {
            Database.Migrate();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Product>(u =>
            {
                u.Property(x => x.Name).IsRequired();
                u.Property(x => x.Price).IsRequired();
            });
        }
    }
}