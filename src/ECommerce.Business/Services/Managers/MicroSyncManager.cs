using System;
using System.Linq;
using ECommerce.Core.DTOs.Micro;
// using ECommerce.Core.Entities.Concrete; // removed: entities live in ECommerce.Entities.Concrete
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Repositories;
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

        public MicroSyncManager(IMicroService microService, IProductRepository productRepository)
        {
            _microService = microService;
            _productRepository = productRepository;
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

            // Varsayılan kategori: mevcut ürünlerden ilk geçerli kategori, yoksa 1
            var existingProducts = (await _productRepository.GetAllAsync()).ToList();
            var defaultCategoryId = existingProducts.Select(p => p.CategoryId).FirstOrDefault(c => c > 0);
            if (defaultCategoryId <= 0)
            {
                defaultCategoryId = 1;
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
                        var newProduct = new Product
                        {
                            Name = string.IsNullOrWhiteSpace(mikroProduct.Name) ? sku : mikroProduct.Name.Trim(),
                            Description = string.Empty,
                            CategoryId = defaultCategoryId,
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
