using System;
using System.Linq;
using ECommerce.Core.DTOs.Micro;
// using ECommerce.Core.Entities.Concrete; // removed: entities live in ECommerce.Entities.Concrete
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Mapping;
using ECommerce.Entities.Concrete;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Amaç: Mikro ERP ile veri senkronizasyonu (ürün, stok, satış vb.)
    /// </summary>
    public class MicroSyncManager
    {
        private readonly IMicroService _microService;
        private readonly IProductRepository _productRepository;
        // NEDEN: Yeni ürün oluşturulurken CategoryCode → CategoryId resolve etmek için
        private readonly IAutoCategoryMappingEngine? _autoMappingEngine;
        private readonly ILogger<MicroSyncManager>? _logger;
        // Config: Mevcut ürünlerin kategorisini üzerine yaz mı?
        private readonly bool _overwriteExistingCategory;

        public MicroSyncManager(
            IMicroService microService,
            IProductRepository productRepository,
            IAutoCategoryMappingEngine? autoMappingEngine = null,
            ILogger<MicroSyncManager>? logger = null,
            IConfiguration? configuration = null)
        {
            _microService = microService;
            _productRepository = productRepository;
            _autoMappingEngine = autoMappingEngine;
            _logger = logger;
            _overwriteExistingCategory = configuration?.GetValue("CategoryMapping:OverwriteExistingCategory", false) ?? false;
        }

        public class ProductSyncResult
        {
            public int TotalProducts { get; set; }
            public int SyncedProducts { get; set; }
            public int CreatedProducts { get; set; }
            public int UpdatedProducts { get; set; }
            public int SkippedProducts { get; set; }
            public int FailedProducts { get; set; }
        }

        /// <summary>
        /// Ürünleri Mikro ERP sistemine senkronize eder.
        /// </summary>
        public void SyncProductsToMikro()
        {
            try
            {
                var products = _productRepository.GetAll();

                foreach (var product in products)
                {
                    _microService.UpdateProduct(new MicroProductDto
                    {
                        Id = product.Id,
                        Sku = product.SKU,
                        Name = product.Name,
                        Stock = product.StockQuantity,
                        Price = product.Price
                    });
                }

                // Başarılı log
                _productRepository.LogSync(new MicroSyncLog
                {
                    EntityType = "Product",
                    Status = "Success",
                    Message = $"Tüm ürünler Mikro ile senkronize edildi ({products.Count()} ürün).",
                    CreatedAt = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                // Hatalı log
                _productRepository.LogSync(new MicroSyncLog
                {
                    EntityType = "Product",
                    Status = "Failed",
                    Message = ex.Message,
                    CreatedAt = DateTime.Now
                });

                // İstersen hata yönetimi için yeniden fırlatabilirsin
                throw;
            }
        }

        /// <summary>
        /// Mikro ERP'den stokları çekerek yerel ürün stoklarını günceller.
        /// SKU eşlemesi yapılır; bulunamazsa atlanır.
        /// </summary>
        public async Task SyncStocksFromMikroAsync()
        {
            var stocks = await _microService.GetStocksAsync();
            int updated = 0;
            foreach (var s in stocks)
            {
                if (string.IsNullOrWhiteSpace(s.Sku)) continue;
                var product = await _productRepository.GetBySkuAsync(s.Sku);
                if (product == null) continue;
                product.StockQuantity = s.Quantity > 0 ? s.Quantity : s.Stock; // iki alan uyumu için
                await _productRepository.UpdateAsync(product);
                updated++;
            }

            await _productRepository.LogSyncAsync(new MicroSyncLog
            {
                EntityType = "Stock",
                Direction = "FromERP",
                Status = "Success",
                Message = $"{updated} ürün stoğu güncellendi",
                CreatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Mikro ERP'den fiyatları çekerek yerel ürün fiyatlarını günceller.
        /// SKU eşlemesi yapılır.
        /// </summary>
        public async Task SyncPricesFromMikroAsync()
        {
            var prices = await _microService.GetPricesAsync();
            int updated = 0;
            foreach (var p in prices)
            {
                if (string.IsNullOrWhiteSpace(p.Sku)) continue;
                var product = await _productRepository.GetBySkuAsync(p.Sku);
                if (product == null) continue;
                product.Price = p.Price;
                await _productRepository.UpdateAsync(product);
                updated++;
            }

            await _productRepository.LogSyncAsync(new MicroSyncLog
            {
                EntityType = "Price",
                Direction = "FromERP",
                Status = "Success",
                Message = $"{updated} ürün fiyatı güncellendi",
                CreatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Mikro ERP'den ürünleri çekerek yerel veritabanına yazar.
        /// Frontend tarafı /api/products üzerinden DB okuduğu için bu metot
        /// ürünlerin arayüze yansımasını sağlar.
        /// </summary>
        public async Task<ProductSyncResult> SyncProductsFromMikroAsync()
        {
            var result = new ProductSyncResult();

            var mikroProducts = (await _microService.GetProductsAsync())?.ToList() ?? new List<MicroProductDto>();
            result.TotalProducts = mikroProducts.Count;

            // Varsayılan kategori: "Diğer" kategorisi (CategorySeeder garanti eder)
            // NEDEN: Eski hardcode CategoryId=1 yerine auto-mapping engine kullan.
            // Engine yoksa bile en kötü 1'e fallback (ama bu olmamalı).
            int fallbackCategoryId = 1;
            if (_autoMappingEngine != null)
            {
                try
                {
                    // "*" wildcard mapping → "Diğer" kategorisi ID'sini al
                    fallbackCategoryId = await _autoMappingEngine.ResolveOrCreateMappingAsync("*");
                }
                catch
                {
                    _logger?.LogWarning("[MicroSyncManager] Wildcard kategori resolve edilemedi, fallback=1");
                }
            }

            foreach (var mikroProduct in mikroProducts)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(mikroProduct.Sku))
                    {
                        result.SkippedProducts++;
                        continue;
                    }

                    var sku = mikroProduct.Sku.Trim();
                    var existing = await _productRepository.GetBySkuAsync(sku);

                    if (existing == null)
                    {
                        // ADIM 8: Yeni ürün — CategoryCode varsa auto-mapping ile resolve et
                        int categoryId = fallbackCategoryId;
                        if (!string.IsNullOrWhiteSpace(mikroProduct.CategoryCode) && _autoMappingEngine != null)
                        {
                            try
                            {
                                categoryId = await _autoMappingEngine.ResolveOrCreateMappingAsync(
                                    mikroProduct.CategoryCode);
                            }
                            catch (Exception catEx)
                            {
                                _logger?.LogWarning(catEx,
                                    "[MicroSyncManager] Kategori resolve hatası: {Sku}, CategoryCode={Code}",
                                    sku, mikroProduct.CategoryCode);
                            }
                        }

                        var newProduct = new Product
                        {
                            Name = string.IsNullOrWhiteSpace(mikroProduct.Name) ? sku : mikroProduct.Name.Trim(),
                            Description = string.Empty,
                            CategoryId = categoryId,
                            Price = mikroProduct.Price,
                            SpecialPrice = null,
                            StockQuantity = Math.Max(0, mikroProduct.StockQuantity > 0 ? mikroProduct.StockQuantity : mikroProduct.Stock),
                            SKU = sku,
                            ImageUrl = string.Empty,
                            Currency = "TRY",
                            IsActive = mikroProduct.IsActive
                        };

                        await _productRepository.AddAsync(newProduct);
                        result.CreatedProducts++;
                        result.SyncedProducts++;
                    }
                    else
                    {
                        existing.Name = string.IsNullOrWhiteSpace(mikroProduct.Name) ? existing.Name : mikroProduct.Name.Trim();
                        existing.Price = mikroProduct.Price;
                        existing.StockQuantity = Math.Max(0, mikroProduct.StockQuantity > 0 ? mikroProduct.StockQuantity : mikroProduct.Stock);
                        existing.IsActive = mikroProduct.IsActive;
                        existing.UpdatedAt = DateTime.UtcNow;

                        // ADIM 8: Mevcut ürün kategori güncellemesi (config ile kontrol)
                        if (_overwriteExistingCategory &&
                            !string.IsNullOrWhiteSpace(mikroProduct.CategoryCode) &&
                            _autoMappingEngine != null)
                        {
                            try
                            {
                                var resolvedCategoryId = await _autoMappingEngine.ResolveOrCreateMappingAsync(
                                    mikroProduct.CategoryCode);
                                existing.CategoryId = resolvedCategoryId;
                            }
                            catch (Exception catEx)
                            {
                                _logger?.LogWarning(catEx,
                                    "[MicroSyncManager] Mevcut ürün kategori güncelleme hatası: {Sku}",
                                    sku);
                            }
                        }

                        await _productRepository.UpdateAsync(existing);
                        result.UpdatedProducts++;
                        result.SyncedProducts++;
                    }
                }
                catch
                {
                    result.FailedProducts++;
                }
            }

            await _productRepository.LogSyncAsync(new MicroSyncLog
            {
                EntityType = "Product",
                Direction = "FromERP",
                Status = result.FailedProducts == 0 ? "Success" : "PartialSuccess",
                Message = $"Mikro ürün sync tamamlandı. Toplam: {result.TotalProducts}, Senkron: {result.SyncedProducts}, Yeni: {result.CreatedProducts}, Güncel: {result.UpdatedProducts}, Atlanan: {result.SkippedProducts}, Hatalı: {result.FailedProducts}",
                CreatedAt = DateTime.UtcNow
            });

            return result;
        }
    }
}
