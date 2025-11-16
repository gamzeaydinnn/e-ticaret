using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using System;
using ECommerce.Data.Context;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Services.Email;
using Microsoft.Extensions.Configuration;
using ECommerce.Core.DTOs.Order;
using System.Linq;


namespace ECommerce.Business.Services.Managers
{
    public class InventoryManager : IInventoryService
    {
        private readonly IProductRepository _productRepository;
        private readonly ECommerceDbContext _context;
        private readonly EmailSender _emailSender;
        private readonly InventorySettings _inventorySettings;
        private readonly IConfiguration _configuration;

        public InventoryManager(
            IProductRepository productRepository,
            ECommerceDbContext context,
            EmailSender emailSender,
            IOptions<InventorySettings> inventoryOptions,
            IConfiguration configuration)
        {
            _productRepository = productRepository;
            _context = context;
            _emailSender = emailSender;
            _inventorySettings = inventoryOptions.Value;
            _configuration = configuration;
        }

        public async Task<bool> IncreaseStockAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return false;
            product.StockQuantity += quantity;
            await _productRepository.UpdateAsync(product);

            // Log
            await LogAsync(product.Id, quantity, InventoryChangeType.Purchase, "Manual increase");
            return true;
        }

        public async Task<bool> DecreaseStockAsync(int productId, int quantity)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.StockQuantity < quantity) return false;
            product.StockQuantity -= quantity;
            await _productRepository.UpdateAsync(product);

            await LogAsync(product.Id, -quantity, InventoryChangeType.Sale, "Manual decrease");
            await CheckThresholdAndNotifyAsync(product);
            return true;
        }

        public async Task<int> GetStockLevelAsync(int productId)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            return product?.StockQuantity ?? 0;
        }
        public async Task<bool> DecreaseStockAsync(int productId, int quantity, InventoryChangeType changeType, string? note = null, int? performedByUserId = null)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null || product.StockQuantity < quantity) return false;

            product.StockQuantity -= quantity;
            await _productRepository.UpdateAsync(product);

            await LogAsync(productId, -quantity, changeType, note, performedByUserId);
            await CheckThresholdAndNotifyAsync(product);
            return true;
        }

        public async Task<bool> IncreaseStockAsync(int productId, int quantity, InventoryChangeType changeType, string? note = null, int? performedByUserId = null)
        {
            var product = await _productRepository.GetByIdAsync(productId);
            if (product == null) return false;

            product.StockQuantity += quantity;
            await _productRepository.UpdateAsync(product);

            await LogAsync(productId, quantity, changeType, note, performedByUserId);
            return true;
        }

        public async Task<(bool Success, string? ErrorMessage)> ValidateStockForOrderAsync(IEnumerable<OrderItemDto> items)
        {
            if (items == null)
            {
                return (false, "Sepet öğeleri gerekli");
            }

            var materialized = items.ToList();
            if (materialized.Count == 0)
            {
                return (false, "Sepet boş olamaz.");
            }

            foreach (var item in materialized)
            {
                if (item.Quantity <= 0)
                {
                    return (false, "Geçersiz miktar");
                }

                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    return (false, $"Ürün bulunamadı: {item.ProductId}");
                }

                if (product.StockQuantity < item.Quantity)
                {
                    return (false, $"Yetersiz stok: {product.Name}");
                }
            }

            return (true, null);
        }

        private async Task LogAsync(int productId, int changeQty, InventoryChangeType type, string? note = null, int? byUserId = null)
        {
            var log = new InventoryLog
            {
                ProductId = productId,
                ChangeQuantity = changeQty,
                ChangeType = type,
                Note = note,
                PerformedByUserId = byUserId,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.InventoryLogs.Add(log);
            await _context.SaveChangesAsync();
        }

        private async Task CheckThresholdAndNotifyAsync(Product product)
        {
            var threshold = Math.Max(1, _inventorySettings.CriticalStockThreshold);
            if (product.StockQuantity <= threshold)
            {
                // DB notification
                var n = new Notification
                {
                    Title = "Düşük Stok Uyarısı",
                    Message = $"{product.Name} için stok {product.StockQuantity} seviyesine düştü (eşik: {threshold}).",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };
                _context.Notifications.Add(n);
                await _context.SaveChangesAsync();

                // E-mail (admin adresi appsettings'ten alınırsa daha iyi; burada varsayılan yoksa atlanır)
                // EmailSender arayüzü basit; AppSettings üzerinden tanımlı From'a gönderilebilir veya Admin:Email alınabilir.
                var adminEmail = _configuration["Admin:Email"];
                if (!string.IsNullOrWhiteSpace(adminEmail))
                {
                    try
                    {
                        await _emailSender.SendEmailAsync(
                            toEmail: adminEmail,
                            subject: "Düşük Stok Uyarısı",
                            body: $"Ürün: {product.Name}\nStok: {product.StockQuantity}\nEşik: {threshold}\nTarih: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}",
                            isHtml: false
                        );
                    }
                    catch
                    {
                        // e-posta yapılandırılmadı ise sessiz geç
                    }
                }
            }
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
