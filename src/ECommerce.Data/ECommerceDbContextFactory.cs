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

            // SQL Server kullanımı
            optionsBuilder.UseSqlServer("Server=my-sqlserver-db.cgbyoi6smmgt.us-east-1.rds.amazonaws.com,1433;Database=ECommerceDb;User Id=gamze;Password=Admin00...;Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;");

            return new ECommerceDbContext(optionsBuilder.Options);
        }
    }
}
