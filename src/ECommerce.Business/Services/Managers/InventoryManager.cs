using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.Cart;
using ECommerce.Core.DTOs.Order;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Config;
using ECommerce.Infrastructure.Services.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;


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
            }

            var grouped = materialized
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();

            var now = DateTime.UtcNow;
            foreach (var entry in grouped)
            {
                var product = await _productRepository.GetByIdAsync(entry.ProductId);
                if (product == null)
                {
                    return (false, $"Ürün bulunamadı: {entry.ProductId}");
                }

                var reservedQuantity = await GetActiveReservedQuantityAsync(entry.ProductId, now);
                var available = product.StockQuantity - reservedQuantity;
                if (available < entry.Quantity)
                {
                    return (false, $"Yetersiz stok: {product.Name}");
                }
            }

            return (true, null);
        }

        public async Task<bool> ReserveStockAsync(Guid clientOrderId, IEnumerable<CartItemDto> items)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var now = DateTime.UtcNow;
            var normalizedItems = items
                .Where(i => i.Quantity > 0)
                .GroupBy(i => i.ProductId)
                .Select(g => new { ProductId = g.Key, Quantity = g.Sum(x => x.Quantity) })
                .ToList();
            if (normalizedItems.Count == 0)
            {
                return false;
            }

            var expiration = now.AddMinutes(Math.Max(1, _inventorySettings.ReservationExpiryMinutes));
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            IDbContextTransaction? transaction = null;
            if (ownsTransaction)
            {
                transaction = await TryBeginTransactionAsync();
                if (transaction == null)
                {
                    ownsTransaction = false;
                }
            }
            try
            {
                var existing = await _context.StockReservations
                    .Where(r => r.ClientOrderId == clientOrderId && !r.IsReleased)
                    .ToListAsync();
                if (existing.Count > 0)
                {
                    foreach (var reservation in existing)
                    {
                        reservation.IsReleased = true;
                        reservation.ExpiresAt = now;
                    }
                    await _context.SaveChangesAsync();
                }

                foreach (var item in normalizedItems)
                {
                    await EnsureProductRowLockedAsync(item.ProductId);

                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (product == null)
                    {
                        await RollbackIfNeededAsync(transaction, ownsTransaction);
                        return false;
                    }

                    var reservedQuantity = await GetActiveReservedQuantityAsync(item.ProductId, now);
                    var available = product.StockQuantity - reservedQuantity;
                    if (available < item.Quantity)
                    {
                        await RollbackIfNeededAsync(transaction, ownsTransaction);
                        return false;
                    }

                    _context.StockReservations.Add(new StockReservation
                    {
                        ClientOrderId = clientOrderId,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        CreatedAt = now,
                        ExpiresAt = expiration,
                        IsReleased = false
                    });
                }

                await _context.SaveChangesAsync();
                await CommitIfNeededAsync(transaction, ownsTransaction);
                return true;
            }
            catch
            {
                await RollbackIfNeededAsync(transaction, ownsTransaction);
                throw;
            }
        }

        public async Task ReleaseReservationAsync(Guid clientOrderId)
        {
            var now = DateTime.UtcNow;
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            IDbContextTransaction? transaction = null;
            if (ownsTransaction)
            {
                transaction = await TryBeginTransactionAsync();
                if (transaction == null)
                {
                    ownsTransaction = false;
                }
            }
            try
            {
                var reservations = await _context.StockReservations
                    .Where(r => r.ClientOrderId == clientOrderId && !r.IsReleased)
                    .ToListAsync();
                if (reservations.Count == 0)
                {
                    await CommitIfNeededAsync(transaction, ownsTransaction);
                    return;
                }

                foreach (var reservation in reservations)
                {
                    reservation.IsReleased = true;
                    reservation.ExpiresAt = now;
                }

                await _context.SaveChangesAsync();
                await CommitIfNeededAsync(transaction, ownsTransaction);
            }
            catch
            {
                await RollbackIfNeededAsync(transaction, ownsTransaction);
                throw;
            }
        }

        public async Task CommitReservationAsync(Guid clientOrderId)
        {
            var now = DateTime.UtcNow;
            var ownsTransaction = _context.Database.CurrentTransaction == null;
            IDbContextTransaction? transaction = null;
            if (ownsTransaction)
            {
                transaction = await TryBeginTransactionAsync();
                if (transaction == null)
                {
                    ownsTransaction = false;
                }
            }
            try
            {
                var reservations = await _context.StockReservations
                    .Where(r => r.ClientOrderId == clientOrderId && !r.IsReleased)
                    .ToListAsync();
                if (reservations.Count == 0)
                {
                    await CommitIfNeededAsync(transaction, ownsTransaction);
                    return;
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.ClientOrderId == clientOrderId);

                foreach (var group in reservations.GroupBy(r => r.ProductId))
                {
                    await EnsureProductRowLockedAsync(group.Key);

                    var totalQuantity = group.Sum(r => r.Quantity);
                    var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == group.Key);
                    if (product == null || product.StockQuantity < totalQuantity)
                    {
                        throw new InvalidOperationException("Stok rezervasyonu onaylanamadı.");
                    }

                    product.StockQuantity -= totalQuantity;
                    await LogAsync(
                        product.Id,
                        -totalQuantity,
                        InventoryChangeType.Sale,
                        order != null ? $"Online Order #{order.OrderNumber}" : $"Client Order {clientOrderId}",
                        order?.UserId);
                    await CheckThresholdAndNotifyAsync(product);
                }

                foreach (var reservation in reservations)
                {
                    reservation.IsReleased = true;
                    reservation.ExpiresAt = now;
                    if (order != null)
                    {
                        reservation.OrderId = order.Id;
                    }
                }

                await _context.SaveChangesAsync();
                await CommitIfNeededAsync(transaction, ownsTransaction);
            }
            catch
            {
                await RollbackIfNeededAsync(transaction, ownsTransaction);
                throw;
            }
        }

        private async Task<int> GetActiveReservedQuantityAsync(int productId, DateTime utcNow)
        {
            var reserved = await _context.StockReservations
                .Where(r => r.ProductId == productId && !r.IsReleased && r.ExpiresAt > utcNow)
                .SumAsync(r => (int?)r.Quantity);
            return reserved ?? 0;
        }

        private Task EnsureProductRowLockedAsync(int productId)
        {
            if (_context.Database.IsSqlServer())
            {
                return _context.Database.ExecuteSqlRawAsync(
                    "SELECT 1 FROM [Products] WITH (UPDLOCK, ROWLOCK) WHERE [Id] = {0}",
                    productId);
            }

            return Task.CompletedTask;
        }

        private static Task CommitIfNeededAsync(IDbContextTransaction? transaction, bool ownsTransaction)
        {
            return ownsTransaction && transaction != null
                ? transaction.CommitAsync()
                : Task.CompletedTask;
        }

        private static Task RollbackIfNeededAsync(IDbContextTransaction? transaction, bool ownsTransaction)
        {
            return ownsTransaction && transaction != null
                ? transaction.RollbackAsync()
                : Task.CompletedTask;
        }

        private async Task<IDbContextTransaction?> TryBeginTransactionAsync()
        {
            try
            {
                return await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("Transactions are not supported", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                throw;
            }
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
