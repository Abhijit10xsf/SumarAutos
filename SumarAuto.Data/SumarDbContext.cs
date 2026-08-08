using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
using SumarAuto.Data.Entities;

namespace SumarAuto.Data
{
    public class SumarDbContext : DbContext
    {
        public SumarDbContext() : base("name=DBEntities")
        {
            Database.SetInitializer<SumarDbContext>(null); // Don't drop or alter existing tables
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<ShippingAddress> ShippingAddresses { get; set; }
        public DbSet<Cart> Carts { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();

            modelBuilder.Entity<Product>().ToTable("Product");
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<ShippingAddress>().ToTable("ShippingAddress");
            modelBuilder.Entity<Cart>().ToTable("Cart");
            modelBuilder.Entity<CartItem>().ToTable("CartItem");
            modelBuilder.Entity<Order>().ToTable("Orders");
            modelBuilder.Entity<OrderDetail>().ToTable("OrderDetails");

            // Ignore calculated properties that are not columns in DB
            modelBuilder.Entity<CartItem>().Ignore(ci => ci.Product);
            modelBuilder.Entity<CartItem>().Ignore(ci => ci.ItemTotal);
            modelBuilder.Entity<User>().Ignore(u => u.Initials);
            modelBuilder.Entity<OrderDetail>().Ignore(od => od.LineTotal);
        }
    }
}
