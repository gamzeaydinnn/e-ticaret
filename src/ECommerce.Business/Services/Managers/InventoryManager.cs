using ECommerce.Entities.Concrete;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Exceptions;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class InventoryManager
    {
        private readonly IProductRepository _productRepository;

        public InventoryManager(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task UpdateStockAsync(int productId, int quantityChange)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) throw new NotFoundException("Product not found");

            product.StockQuantity += quantityChange;
            if (product.StockQuantity < 0) product.StockQuantity = 0;

            await _productRepository.UpdateAsync(product);
        }
    }
}
