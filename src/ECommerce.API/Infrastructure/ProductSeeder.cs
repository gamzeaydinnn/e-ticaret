using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ECommerce.API.Infrastructure
{
    public static class ProductSeeder
    {
        public static Task SeedAsync(IServiceProvider services)
        {
            // Demo ürün seed'i kaldırıldı — ürünler Mikro ERP senkronizasyonu ile gelir.
            Console.WriteLine("ℹ️ ProductSeeder: atlandı (ürünler Mikro'dan senkronize edilir)");
            return Task.CompletedTask;
        }
    }
}
