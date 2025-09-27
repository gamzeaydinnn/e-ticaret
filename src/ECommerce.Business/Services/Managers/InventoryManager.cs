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
          /*
          Stok yönetimi & eşzamanlılık (kritik algoritma)
         Önemli kavram: satış sırasında rezervasyon (reserve) → ödeme onayı gelince kesin düşüm (decrement). 
         Bu, oversell(fazla satma) riskini azaltır.
                Yaklaşım 1 — Optimistic concurrency (tercih)
	•    Inventory tablosunda RowVersion kullan. Checkout işleminde:
		1. DB’den inventory oku (Quantity - Reserved >= requested?).
		2. Reserved += requested yap.
		3. SaveChanges() — eğer DbUpdateConcurrencyException gelirse, tekrar oku ve retry (örn. 3 defa).
        
        
          public async Task ReserveStockAsync(long productId, int qty) {
            for (int attempt=0; attempt<3; attempt++) {
            var inv = await _db.Inventories.SingleAsync(i => i.ProductId==productId);
            if (inv.Quantity - inv.Reserved < qty) throw new InvalidOperationException("Yetersiz stok");
             inv.Reserved += qty;
            try {
              await _db.SaveChangesAsync();
              return;
             } catch (DbUpdateConcurrencyException) {
             // retry
             }
              }
              throw new Exception("Stok rezerve edilemedi, tekrar deneyin");
            }*/

    }
}
