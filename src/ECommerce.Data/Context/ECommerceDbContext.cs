using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;



namespace ECommerce.Data.Context

{

    public class ECommerceDbContext : DbContextnamespace ECommerce.Data.Context

    {

        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options){

        {

        }    public class ECommerceDbContext : DbContextnamespace ECommerce.Data.Contextusing ECommerce.Entities.Concrete;



        // TODO: Add DbSets    {

        // public DbSet<User> Users { get; set; }

        // public DbSet<Product> Products { get; set; }        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options){

        // public DbSet<Category> Categories { get; set; }

        // public DbSet<Order> Orders { get; set; }        {

        // public DbSet<OrderItem> OrderItems { get; set; }

        }    public class ECommerceDbContext : DbContextusing ECommerce.Entities.Concrete;

        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {

            // TODO: Add entity configurations

            base.OnModelCreating(modelBuilder);        // TODO: Add DbSets    {

        }

    }        // public DbSet<User> Users { get; set; }

}
        // public DbSet<Product> Products { get; set; }        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)namespace ECommerce.Data.Context

        // public DbSet<Category> Categories { get; set; }

        // public DbSet<Order> Orders { get; set; }        {

        // public DbSet<OrderItem> OrderItems { get; set; }

        }{using ECommerce.Entities.Concrete;

        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {

            // TODO: Add entity configurations

            base.OnModelCreating(modelBuilder);        // TODO: Add DbSets    public class ECommerceDbContext : DbContext

        }

    }        // public DbSet<User> Users { get; set; }

}
        // public DbSet<Product> Products { get; set; }    {namespace ECommerce.Data.Context

        // public DbSet<Category> Categories { get; set; }

        // public DbSet<Order> Orders { get; set; }        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)

        // public DbSet<OrderItem> OrderItems { get; set; }

        {{using ECommerce.Entities.Concrete;

        protected override void OnModelCreating(ModelBuilder modelBuilder)

        {        }

            // TODO: Add entity configurations

            base.OnModelCreating(modelBuilder);    public class ECommerceDbContext : DbContext

        }

    }        public DbSet<User> Users { get; set; }

}
        public DbSet<Category> Categories { get; set; }    {namespace ECommerce.Data.Context

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)

        public DbSet<OrderItem> OrderItems { get; set; }

    }        {{using ECommerce.Entities.Concrete;using ECommerce.Entities.Concrete;

}
        }

    public class ECommerceDbContext : DbContext

        public DbSet<User> Users { get; set; }

        public DbSet<Category> Categories { get; set; }    {namespace ECommerce.Data.Context

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)

        public DbSet<OrderItem> OrderItems { get; set; }

    }        {{

}
        }

    public class ECommerceDbContext : DbContext

        public DbSet<User> Users { get; set; }

        public DbSet<Category> Categories { get; set; }    {namespace ECommerce.Data.Contextnamespace ECommerce.Data.Context

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)

        public DbSet<OrderItem> OrderItems { get; set; }

    }        {{{

}
        }

    public class ECommerceDbContext : DbContext    public class ECommerceDbContext : IdentityDbContext<User, IdentityRole<int>, int>

        public DbSet<User> Users { get; set; }

        public DbSet<Category> Categories { get; set; }    {    {

        public DbSet<Product> Products { get; set; }

        public DbSet<Order> Orders { get; set; }        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)        public ECommerceDbContext(DbContextOptions<ECommerceDbContext> options) : base(options)

        public DbSet<OrderItem> OrderItems { get; set; }

    }        {        {

}
        }        }



        // DbSets        // DbSets

        public DbSet<User> Users { get; set; }        public virtual DbSet<Category> Categories { get; set; }

        public DbSet<Category> Categories { get; set; }        public virtual DbSet<Product> Products { get; set; }

        public DbSet<Product> Products { get; set; }        public virtual DbSet<Order> Orders { get; set; }

        public DbSet<Order> Orders { get; set; }        public virtual DbSet<OrderItem> OrderItems { get; set; }

        public DbSet<OrderItem> OrderItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)

        protected override void OnModelCreating(ModelBuilder modelBuilder)        {

        {            base.OnModelCreating(modelBuilder);

            base.OnModelCreating(modelBuilder);

            // User Configuration

            // User Configuration            modelBuilder.Entity<User>(entity =>

            modelBuilder.Entity<User>(entity =>            {

            {                entity.ToTable("Users");

                entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();                entity.Property(e => e.FirstName).HasMaxLength(50).IsRequired();

                entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();                entity.Property(e => e.LastName).HasMaxLength(50).IsRequired();

                entity.Property(e => e.Email).HasMaxLength(100).IsRequired();                entity.Property(e => e.Address).HasMaxLength(500);

            });                entity.Property(e => e.City).HasMaxLength(100);

            });

            // Category Configuration

            modelBuilder.Entity<Category>(entity =>            // Category Configuration

            {            modelBuilder.Entity<Category>(entity =>

                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();            {

                entity.Property(e => e.Description).HasMaxLength(500);                entity.ToTable("Categories");

            });                entity.Property(e => e.Name).HasMaxLength(100).IsRequired();

                entity.Property(e => e.Description).HasMaxLength(500);

            // Product Configuration                

            modelBuilder.Entity<Product>(entity =>                entity.HasOne(e => e.Parent)

            {                    .WithMany(e => e.SubCategories)

                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();                    .HasForeignKey(e => e.ParentId)

                entity.Property(e => e.Description).HasMaxLength(1000);                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");            });



                entity.HasOne(e => e.Category)            // Product Configuration

                    .WithMany(e => e.Products)            modelBuilder.Entity<Product>(entity =>

                    .HasForeignKey(e => e.CategoryId);            {

            });                entity.ToTable("Products");

                entity.Property(e => e.Name).HasMaxLength(200).IsRequired();

            // Order Configuration                entity.Property(e => e.Description).HasMaxLength(1000);

            modelBuilder.Entity<Order>(entity =>                entity.Property(e => e.SKU).HasMaxLength(50).IsRequired();

            {                entity.Property(e => e.Brand).HasMaxLength(100);

                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

                entity.Property(e => e.Status).HasMaxLength(50);                entity.HasOne(e => e.Category)

                    .WithMany(e => e.Products)

                entity.HasOne(e => e.User)                    .HasForeignKey(e => e.CategoryId)

                    .WithMany(e => e.Orders)                    .OnDelete(DeleteBehavior.Restrict);

                    .HasForeignKey(e => e.UserId);

            });                entity.HasIndex(e => e.SKU).IsUnique();

            });

            // OrderItem Configuration

            modelBuilder.Entity<OrderItem>(entity =>            // Order Configuration

            {            modelBuilder.Entity<Order>(entity =>

                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");            {

                entity.Property(e => e.TotalPrice).HasColumnType("decimal(18,2)");                entity.ToTable("Orders");

                entity.Property(e => e.OrderNumber).HasMaxLength(50).IsRequired();

                entity.HasOne(e => e.Order)                entity.Property(e => e.ShippingAddress).HasMaxLength(500).IsRequired();

                    .WithMany(e => e.OrderItems)                entity.Property(e => e.ShippingCity).HasMaxLength(100).IsRequired();

                    .HasForeignKey(e => e.OrderId);

                entity.HasOne(e => e.User)

                entity.HasOne(e => e.Product)                    .WithMany(e => e.Orders)

                    .WithMany()                    .HasForeignKey(e => e.UserId)

                    .HasForeignKey(e => e.ProductId);                    .OnDelete(DeleteBehavior.Restrict);

            });

        }                entity.HasIndex(e => e.OrderNumber).IsUnique();

    }            });

}
            // OrderItem Configuration
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.ToTable("OrderItems");
                
                entity.HasOne(e => e.Order)
                    .WithMany(e => e.OrderItems)
                    .HasForeignKey(e => e.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                    .WithMany(e => e.OrderItems)
                    .HasForeignKey(e => e.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Seed Data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Categories
            modelBuilder.Entity<Category>().HasData(
                new Category
                {
                    Id = 1,
                    Name = "Elektronik",
                    Description = "Elektronik ürünler",
                    SortOrder = 1,
                    CreatedDate = new DateTime(2025, 9, 21, 0, 0, 0, DateTimeKind.Utc)
                },
                new Category
                {
                    Id = 2,
                    Name = "Giyim",
                    Description = "Giyim ürünleri",
                    SortOrder = 2,
                    CreatedDate = new DateTime(2025, 9, 21, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}