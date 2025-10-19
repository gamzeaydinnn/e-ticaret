using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ECommerce.Data.Context;
using Microsoft.Extensions.Configuration;
using System.IO;
using System;

namespace ECommerce.Data
{
    public class ECommerceDbContextFactory : IDesignTimeDbContextFactory<ECommerceDbContext>
    {
        public ECommerceDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<ECommerceDbContext>();

            // appsettings.json yükle (tasarım zamanı)
            var basePath = Directory.GetCurrentDirectory();
            var apiPath = Path.Combine(basePath, "..", "ECommerce.API");
            var selectedBase = File.Exists(Path.Combine(basePath, "appsettings.json")) ? basePath : apiPath;

            var config = new ConfigurationBuilder()
                .SetBasePath(selectedBase)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            var cs = config.GetConnectionString("DefaultConnection")
                     ?? "Server=localhost;Database=ECommerceDb;Trusted_Connection=True;TrustServerCertificate=True;";
            optionsBuilder.UseSqlServer(cs);

            return new ECommerceDbContext(optionsBuilder.Options);
        }
    }
}
