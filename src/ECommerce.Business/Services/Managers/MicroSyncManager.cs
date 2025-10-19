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
    }
}
