using Microsoft.EntityFrameworkCore;
using ECommerce.Core.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.Extensions.Logging;

namespace ECommerce.Data.Repositories
{
    /// <summary>
    /// Sepet repository implementasyonu
    /// Hem kayıtlı kullanıcı (UserId) hem misafir kullanıcı (CartToken) destekler
    /// SOLID: Single Responsibility - Sadece sepet verisi erişimi
    /// </summary>
    public class CartRepository : BaseRepository<CartItem>, ICartRepository
    {
        private readonly ILogger<CartRepository>? _logger;

        public CartRepository(ECommerceDbContext context, ILogger<CartRepository>? logger = null) 
            : base(context)
        {
            _logger = logger;
        }

        #region Kayıtlı Kullanıcı Metodları (UserId bazlı)

        /// <summary>
        /// Kayıtlı kullanıcının aktif sepet öğelerini getirir
        /// Include: Product ve Category bilgileri (eager loading)
        /// </summary>
        public async Task<IEnumerable<CartItem>> GetByUserIdAsync(int userId)
        {
            return await _dbSet
                .Include(c => c.Product)
                    .ThenInclude(p => p!.Category)
                .Include(c => c.ProductVariant) // Varyant desteği
                .Where(c => c.UserId == userId && c.IsActive)
                .AsNoTracking() // Read-only, performans için
                .ToListAsync();
        }

        /// <summary>
        /// Kullanıcının sepetinde belirli bir ürünü bulur
        /// Varyant kontrolü: Aynı ürünün farklı varyantları ayrı satır olarak tutulur
        /// </summary>
        public async Task<CartItem?> GetByUserAndProductAsync(int userId, int productId)
        {
            return await _dbSet
                .FirstOrDefaultAsync(c => 
                    c.UserId == userId && 
                    c.ProductId == productId && 
                    c.IsActive);
        }

        /// <summary>
        /// Kullanıcının sepetinden belirli bir ürünü siler (soft delete)
        /// </summary>
        public async Task RemoveByUserAndProductAsync(int userId, int productId)
        {
            var cartItem = await GetByUserAndProductAsync(userId, productId);
            if (cartItem != null)
            {
                // Soft delete - IsActive = false
                cartItem.IsActive = false;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                
                _logger?.LogInformation(
                    "[CART] Ürün sepetten silindi. UserId: {UserId}, ProductId: {ProductId}", 
                    userId, productId);
            }
        }

        /// <summary>
        /// Kayıtlı kullanıcının tüm sepetini temizler (soft delete)
        /// </summary>
        public async Task ClearCartAsync(int userId)
        {
            var cartItems = await _dbSet
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();

            foreach (var item in cartItems)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            _logger?.LogInformation(
                "[CART] Kullanıcı sepeti temizlendi. UserId: {UserId}, Silinen: {Count}", 
                userId, cartItems.Count);
        }

        #endregion

        #region Misafir Kullanıcı Metodları (CartToken bazlı)

        /// <summary>
        /// Misafir kullanıcının sepet öğelerini CartToken ile getirir
        /// CartToken: Frontend'de oluşturulan UUID, localStorage'da saklanır
        /// </summary>
        public async Task<IEnumerable<CartItem>> GetByCartTokenAsync(string cartToken)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
            {
                _logger?.LogWarning("[CART] GetByCartTokenAsync: Boş token!");
                return Enumerable.Empty<CartItem>();
            }

            return await _dbSet
                .Include(c => c.Product)
                    .ThenInclude(p => p!.Category)
                .Include(c => c.ProductVariant)
                .Where(c => c.CartToken == cartToken && c.IsActive)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Misafir kullanıcının sepetinde belirli bir ürün/varyantı bulur
        /// Varyant desteği: Aynı ürünün farklı varyantları ayrı satır
        /// </summary>
        public async Task<CartItem?> GetByTokenAndProductAsync(
            string cartToken, 
            int productId, 
            int? productVariantId = null)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
                return null;

            return await _dbSet
                .FirstOrDefaultAsync(c => 
                    c.CartToken == cartToken && 
                    c.ProductId == productId && 
                    c.ProductVariantId == productVariantId && // null == null true döner
                    c.IsActive);
        }

        /// <summary>
        /// Misafir kullanıcının tüm sepetini temizler
        /// Token geçersiz kılınır (merge sonrası veya manuel temizlik)
        /// </summary>
        public async Task ClearCartByTokenAsync(string cartToken)
        {
            if (string.IsNullOrWhiteSpace(cartToken))
                return;

            var cartItems = await _dbSet
                .Where(c => c.CartToken == cartToken && c.IsActive)
                .ToListAsync();

            foreach (var item in cartItems)
            {
                item.IsActive = false;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            
            _logger?.LogInformation(
                "[CART] Misafir sepet temizlendi. Token: {Token}, Silinen: {Count}", 
                cartToken[..8] + "...", cartItems.Count);
        }

        #endregion

        #region Merge Operasyonları (Guest → User Transferi)

        /// <summary>
        /// Misafir sepetini kayıtlı kullanıcıya transfer eder
        /// Login sonrası çağrılır - TRANSACTION ile atomik işlem
        /// 
        /// Çakışma stratejisi:
        /// - Aynı ürün+varyant kombinasyonu varsa: Miktarları topla
        /// - Yoksa: Yeni satır olarak ekle
        /// </summary>
        public async Task<int> MergeGuestCartToUserAsync(string cartToken, int userId)
        {
            if (string.IsNullOrWhiteSpace(cartToken) || userId <= 0)
            {
                _logger?.LogWarning(
                    "[CART-MERGE] Geçersiz parametreler. Token: {Token}, UserId: {UserId}", 
                    cartToken, userId);
                return 0;
            }

            // Execution strategy ile transaction kullan
            var strategy = _context.Database.CreateExecutionStrategy();
            
            return await strategy.ExecuteAsync(async () =>
            {
                // Transaction başlat - atomik işlem garantisi
                using var transaction = await _context.Database.BeginTransactionAsync();
                
                try
                {
                    // 1. Misafir sepet öğelerini al
                    var guestItems = await _dbSet
                        .Where(c => c.CartToken == cartToken && c.IsActive)
                        .ToListAsync();

                if (!guestItems.Any())
                {
                    _logger?.LogInformation("[CART-MERGE] Misafir sepet boş, transfer yok.");
                    return 0;
                }

                // 2. Kullanıcının mevcut sepetini al (çakışma kontrolü için)
                var userItems = await _dbSet
                    .Where(c => c.UserId == userId && c.IsActive)
                    .ToListAsync();

                int transferredCount = 0;

                foreach (var guestItem in guestItems)
                {
                    // Çakışma kontrolü: Aynı ürün+varyant var mı?
                    var existingUserItem = userItems.FirstOrDefault(u => 
                        u.ProductId == guestItem.ProductId && 
                        u.ProductVariantId == guestItem.ProductVariantId);

                    if (existingUserItem != null)
                    {
                        // Çakışma var: Miktarları topla
                        existingUserItem.Quantity += guestItem.Quantity;
                        existingUserItem.UpdatedAt = DateTime.UtcNow;
                        
                        _logger?.LogDebug(
                            "[CART-MERGE] Miktar güncellendi. ProductId: {ProductId}, Yeni Miktar: {Qty}", 
                            existingUserItem.ProductId, existingUserItem.Quantity);
                    }
                    else
                    {
                        // Çakışma yok: Yeni satır olarak ekle
                        var newItem = new CartItem
                        {
                            UserId = userId,
                            ProductId = guestItem.ProductId,
                            ProductVariantId = guestItem.ProductVariantId,
                            Quantity = guestItem.Quantity,
                            CartToken = null, // Artık kayıtlı kullanıcı
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        await _dbSet.AddAsync(newItem);
                        transferredCount++;
                    }

                    // Misafir öğeyi pasifleştir
                    guestItem.IsActive = false;
                    guestItem.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger?.LogInformation(
                    "[CART-MERGE] ✅ Transfer tamamlandı. UserId: {UserId}, Transfer: {Count}, Toplam Guest: {Total}", 
                    userId, transferredCount, guestItems.Count);

                return guestItems.Count;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger?.LogError(ex, "[CART-MERGE] ❌ Transfer başarısız! UserId: {UserId}", userId);
                throw;
            }
            });
        }

        #endregion

        #region Ortak Metodlar

        /// <summary>
        /// Sepet öğesini ID ile siler (soft delete)
        /// </summary>
        public async Task RemoveCartItemAsync(int cartItemId)
        {
            var cartItem = await _dbSet.FindAsync(cartItemId);
            if (cartItem != null)
            {
                cartItem.IsActive = false;
                cartItem.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Sistemdeki toplam aktif sepet öğesi sayısı (admin dashboard için)
        /// </summary>
        public async Task<int> GetCartCountAsync()
        {      
            return await _dbSet.CountAsync(c => c.IsActive);
        }

        #endregion
    }
}
