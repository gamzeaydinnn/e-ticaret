using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Entities.Concrete;
using ECommerce.Core.Interfaces;
using ECommerce.Infrastructure.Services;
//Amaç: Mikro ERP ile veri senkronizasyonu (stok, ürün, satış).
namespace ECommerce.Business.Services.Managers
{
    public class MikroSyncManager
    {
        private readonly IMicroService _mikroService;
        private readonly IProductRepository _productRepository;

        public MikroSyncManager(IMicroService mikroService, IProductRepository productRepository)
        {
            _mikroService = mikroService;
            _productRepository = productRepository;
        }

        public void SyncProductsToMikro()
        {
            var products = _productRepository.GetAll();
            foreach (var product in products)
            {
                _mikroService.UpdateProduct(new MicroProductDto
                {
                    Id = product.Id,
                    Name = product.Name,
                    Stock = product.StockQuantity,
                    Price = product.Price
                });
            }
        }
    }
}
