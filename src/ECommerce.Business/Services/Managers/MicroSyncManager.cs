using ECommerce.Core.DTOs;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Entities.Concrete;
using ECommerce.Core.Interfaces;
//Amaç: Mikro ERP ile veri senkronizasyonu (stok, ürün, satış).
namespace ECommerce.Business.Services.Managers
{
    public class MicroSyncManager
    {
        private readonly IMicroService _microService;
        private readonly IProductRepository _productRepository;

        public MicroSyncManager(IMicroService microService, IProductRepository productRepository)
        {
            _microService = microService;
            _productRepository = productRepository;
        }

        public void SyncProductsToMikro()
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
        }
    }
}
