using System;
using System.Linq;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Entities.Concrete;
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
    }
}
