using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Managers
{
    public class InventoryManager : IInventoryService
    {
        private readonly IProductRepository _productRepository;

        public InventoryManager(IProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        public async Task<bool> IncreaseStockAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return false;
            product.StockQuantity += quantity;
            await _productRepository.UpdateAsync(product);
            return true;
        }

        public async Task<bool> DecreaseStockAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.StockQuantity < quantity) return false;
            product.StockQuantity -= quantity;
            await _productRepository.UpdateAsync(product);
            return true;
        }

        public async Task<int> GetStockLevelAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product?.StockQuantity ?? 0;
        }
    }
}
