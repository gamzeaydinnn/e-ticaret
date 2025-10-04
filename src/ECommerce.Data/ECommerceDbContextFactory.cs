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

            // 🔹 BURAYA Azure SQL bağlantı cümleni yaz:
            optionsBuilder.UseSqlServer("Server=localhost,1433;Database=ECommerceDb;User Id=sa;Password=Admin123!;TrustServerCertificate=True;");

            return new ECommerceDbContext(optionsBuilder.Options);
        }
    }
}
