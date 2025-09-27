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
