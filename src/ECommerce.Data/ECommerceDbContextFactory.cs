using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ECommerce.Data.Context;

namespace ECommerce.Data
{
    public class ECommerceDbContextFactory : IDesignTimeDbContextFactory<ECommerceDbContext>
    {
        public ECommerceDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ECommerceDbContext>();

            // SQLite bağlantısı
            optionsBuilder.UseSqlite("Data Source=ECommerce.db");

            return new ECommerceDbContext(optionsBuilder.Options);
        }
    }
}
