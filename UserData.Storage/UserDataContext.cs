using Microsoft.EntityFrameworkCore;
using UserData.Storage.Models;

namespace UserData.Storage
{
    public class UserDataContext : DbContext
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<Order> Orders { get; set; } = null!;
        public DbSet<OrderItem> OrderItems { get; set; } = null!;

        public UserDataContext(DbContextOptions<UserDataContext> options)
            : base(options)
        {
            Database.Migrate();
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>(u =>
            {
                u.Property(x => x.Login).IsRequired();
                u.Property(x => x.Password).IsRequired();
                u.HasIndex(x => x.Login).IsUnique();
                u.HasMany(user => user.Orders)
                    .WithOne(order => order.User)
                    .HasForeignKey(order => order.UserId);
            })
            .Entity<Order>(o =>
            {
                o.HasMany(order => order.Items)
                    .WithOne(item => item.Order)
                    .HasForeignKey(item => item.OrderId);
            });
        }
    }
}