using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;

namespace ECommerce.API.Infrastructure
{
    public static class ProductSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var dbContext = services.GetRequiredService<ECommerceDbContext>();

            // Kategoriler zaten varsa çıkış yap
            if (dbContext.Categories.Any())
                return;

            // Kategorileri ekle
            var categories = new List<Category>
            {
                new Category
                {
                    Name = "Et ve Et Ürünleri",
                    Description = "Taze et ve şarküteri ürünleri",
                    ImageUrl = "/images/dana-kusbasi.jpg",
                    Slug = "et-ve-et-urunleri",
                    SortOrder = 1,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Süt ve Süt Ürünleri",
                    Description = "Süt, peynir, yoğurt ve türevleri",
                    ImageUrl = "/images/ozel-fiyat-koy-sutu.png",
                    Slug = "sut-ve-sut-urunleri",
                    SortOrder = 2,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Meyve ve Sebze",
                    Description = "Taze meyve ve sebzeler",
                    ImageUrl = "/images/domates.webp",
                    Slug = "meyve-ve-sebze",
                    SortOrder = 3,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "İçecekler",
                    Description = "Soğuk ve sıcak içecekler",
                    ImageUrl = "/images/coca-cola.jpg",
                    Slug = "icecekler",
                    SortOrder = 4,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Atıştırmalık",
                    Description = "Cipsi, kraker ve atıştırmalıklar",
                    ImageUrl = "/images/tahil-cipsi.jpg",
                    Slug = "atistirmalik",
                    SortOrder = 5,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Category
                {
                    Name = "Temizlik",
                    Description = "Ev temizlik ürünleri",
                    ImageUrl = "/images/yeşil-cif-krem.jpg",
                    Slug = "temizlik",
                    SortOrder = 6,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await dbContext.Categories.AddRangeAsync(categories);
            await dbContext.SaveChangesAsync();

            // Kategorileri ID'leriyle birlikte al
            var savedCategories = await dbContext.Categories.ToListAsync();
            var catById = savedCategories.ToDictionary(c => c.Slug, c => c.Id);

            // Ürünleri ekle
            var products = new List<Product>
            {
                new Product
                {
                    Name = "Dana Kuşbaşı",
                    Description = "Taze dana eti kuşbaşı",
                    CategoryId = catById["et-ve-et-urunleri"],
                    Price = 89.90m,
                    StockQuantity = 25,
                    ImageUrl = "/images/dana-kusbasi.jpg",
                    Slug = "dana-kusbasi",
                    SKU = "ET-001",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Kuzu İncik",
                    Description = "Taze kuzu incik eti",
                    CategoryId = catById["et-ve-et-urunleri"],
                    Price = 95.50m,
                    StockQuantity = 15,
                    ImageUrl = "/images/kuzu-incik.webp",
                    Slug = "kuzu-incik",
                    SKU = "ET-002",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Sucuk 250gr",
                    Description = "Geleneksel sucuk",
                    CategoryId = catById["et-ve-et-urunleri"],
                    Price = 24.90m,
                    StockQuantity = 30,
                    ImageUrl = "/images/sucuk.jpg",
                    Slug = "sucuk-250gr",
                    SKU = "ET-003",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Pınar Süt 1L",
                    Description = "Taze tam yağlı süt",
                    CategoryId = catById["sut-ve-sut-urunleri"],
                    Price = 12.50m,
                    StockQuantity = 50,
                    ImageUrl = "/images/ozel-fiyat-koy-sutu.png",
                    Slug = "pinar-sut-1l",
                    SKU = "SUT-001",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Şek Kaşar Peyniri 200gr",
                    Description = "Eski kaşar peynir",
                    CategoryId = catById["sut-ve-sut-urunleri"],
                    Price = 35.90m,
                    StockQuantity = 20,
                    ImageUrl = "/images/sek-kasar-peyniri-200-gr-38be46-1650x1650.jpg",
                    Slug = "sek-kasar-peyniri-200gr",
                    SKU = "SUT-002",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Domates Kg",
                    Description = "Taze domates",
                    CategoryId = catById["meyve-ve-sebze"],
                    Price = 8.75m,
                    StockQuantity = 100,
                    ImageUrl = "/images/domates.webp",
                    Slug = "domates-kg",
                    SKU = "SEB-001",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Salatalık Kg",
                    Description = "Taze salatalık",
                    CategoryId = catById["meyve-ve-sebze"],
                    Price = 6.50m,
                    StockQuantity = 80,
                    ImageUrl = "/images/salatalik.jpg",
                    Slug = "salatalik-kg",
                    SKU = "SEB-002",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Bulgur 1 Kg",
                    Description = "Pilavlık bulgur",
                    CategoryId = catById["meyve-ve-sebze"],
                    Price = 15.90m,
                    StockQuantity = 40,
                    ImageUrl = "/images/bulgur.png",
                    Slug = "bulgur-1-kg",
                    SKU = "BAK-001",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Coca Cola 330ml",
                    Description = "Coca Cola teneke kutu",
                    CategoryId = catById["icecekler"],
                    Price = 5.50m,
                    StockQuantity = 75,
                    ImageUrl = "/images/coca-cola.jpg",
                    Slug = "coca-cola-330ml",
                    SKU = "ICE-001",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Lipton Ice Tea 330ml",
                    Description = "Şeftali aromalı ice tea",
                    CategoryId = catById["icecekler"],
                    Price = 4.75m,
                    StockQuantity = 60,
                    ImageUrl = "/images/lipton-ice-tea.jpg",
                    Slug = "lipton-ice-tea-330ml",
                    SKU = "ICE-002",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Nescafe 200gr",
                    Description = "Klasik nescafe",
                    CategoryId = catById["icecekler"],
                    Price = 45.90m,
                    StockQuantity = 25,
                    ImageUrl = "/images/nescafe.jpg",
                    Slug = "nescafe-200gr",
                    SKU = "ICE-003",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Tahıl Cipsi 150gr",
                    Description = "Çıtır tahıl cipsi",
                    CategoryId = catById["atistirmalik"],
                    Price = 12.90m,
                    StockQuantity = 35,
                    ImageUrl = "/images/tahil-cipsi.jpg",
                    Slug = "tahil-cipsi-150gr",
                    SKU = "ATI-001",
                    Currency = "TRY",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                },
                new Product
                {
                    Name = "Cif Krem Temizleyici",
                    Description = "Mutfak temizleyici",
                    CategoryId = catById["temizlik"],
                    Price = 15.90m,
                    StockQuantity = 5,
                    ImageUrl = "/images/yeşil-cif-krem.jpg",
                    Slug = "cif-krem-temizleyici",
                    SKU = "TEM-001",
                    Currency = "TRY",
                    IsActive = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await dbContext.Products.AddRangeAsync(products);
            await dbContext.SaveChangesAsync();
        }
    }
}
