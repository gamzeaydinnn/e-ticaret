using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
                    Id = 1,
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
                    Id = 2,
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
                    Id = 3,
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
                    Id = 4,
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
                    Id = 5,
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
                    Id = 6,
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

            // Ürünleri ekle
            var products = new List<Product>
            {
                new Product
                {
                    Id = 1,
                    Name = "Dana Kuşbaşı",
                    Description = "Taze dana eti kuşbaşı",
                    CategoryId = 1,
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
                    Id = 2,
                    Name = "Kuzu İncik",
                    Description = "Taze kuzu incik eti",
                    CategoryId = 1,
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
                    Id = 3,
                    Name = "Sucuk 250gr",
                    Description = "Geleneksel sucuk",
                    CategoryId = 1,
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
                    Id = 4,
                    Name = "Pınar Süt 1L",
                    Description = "Taze tam yağlı süt",
                    CategoryId = 2,
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
                    Id = 5,
                    Name = "Şek Kaşar Peyniri 200gr",
                    Description = "Eski kaşar peynir",
                    CategoryId = 2,
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
                    Id = 6,
                    Name = "Domates Kg",
                    Description = "Taze domates",
                    CategoryId = 3,
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
                    Id = 7,
                    Name = "Salatalık Kg",
                    Description = "Taze salatalık",
                    CategoryId = 3,
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
                    Id = 8,
                    Name = "Bulgur 1 Kg",
                    Description = "Pilavlık bulgur",
                    CategoryId = 3,
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
                    Id = 9,
                    Name = "Coca Cola 330ml",
                    Description = "Coca Cola teneke kutu",
                    CategoryId = 4,
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
                    Id = 10,
                    Name = "Lipton Ice Tea 330ml",
                    Description = "Şeftali aromalı ice tea",
                    CategoryId = 4,
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
                    Id = 11,
                    Name = "Nescafe 200gr",
                    Description = "Klasik nescafe",
                    CategoryId = 4,
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
                    Id = 12,
                    Name = "Tahıl Cipsi 150gr",
                    Description = "Çıtır tahıl cipsi",
                    CategoryId = 5,
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
                    Id = 13,
                    Name = "Cif Krem Temizleyici",
                    Description = "Mutfak temizleyici",
                    CategoryId = 6,
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
