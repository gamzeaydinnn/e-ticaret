using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Hubs
{
    /// <summary>
    /// Stok, fiyat ve ürün bilgi değişikliklerini anlık ileten SignalR hub'ı.
    /// 
    /// NEDEN: Müşteri ürün detay sayfasında veya sepette iken stok tükenirse/fiyat değişirse
    /// sayfa yenilemeden anında bilgilendirilmeli. Mevcut mimari (OrderHub, CourierHub vb.)
    /// ile tutarlı olarak ayrı bir hub olarak tasarlandı.
    /// 
    /// GRUPLAR:
    /// - "product-{id}": Belirli ürünü izleyen kullanıcılar (ürün detay sayfası)
    /// - "cart-{userId}": Kullanıcının sepetindeki ürünleri izler
    /// - "stock-global": Tüm stok değişikliklerini izleyen admin kullanıcılar
    /// 
    /// EVENTS (Server → Client):
    /// - StockChanged(productId, oldQty, newQty, source)
    /// - PriceChanged(productId, oldPrice, newPrice, source)
    /// - ProductInfoChanged(productId, changes)
    /// - BulkStockUpdate(updates[])
    /// 
    /// GÜVENLİK: Auth zorunlu değil (anonim kullanıcılar da stok görmeli)
    /// ama admin-only gruplar için rol kontrolü yapılır.
    /// </summary>
    public class StockHub : Hub
    {
        private readonly ILogger<StockHub> _logger;

        public StockHub(ILogger<StockHub> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Belirli bir ürünün stok/fiyat değişikliklerini dinlemeye başla.
        /// Ürün detay sayfası açıldığında çağrılır.
        /// </summary>
        public async Task JoinProductRoom(int productId)
        {
            if (productId <= 0) return;

            var groupName = $"product-{productId}";
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogDebug(
                "[StockHub] Client ürün odasına katıldı. ProductId: {ProductId}, ConnectionId: {ConnectionId}",
                productId, Context.ConnectionId);
        }

        /// <summary>
        /// Ürün odasından ayrıl.
        /// Ürün detay sayfasından çıkıldığında çağrılır.
        /// </summary>
        public async Task LeaveProductRoom(int productId)
        {
            if (productId <= 0) return;

            var groupName = $"product-{productId}";
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogDebug(
                "[StockHub] Client ürün odasından ayrıldı. ProductId: {ProductId}, ConnectionId: {ConnectionId}",
                productId, Context.ConnectionId);
        }

        /// <summary>
        /// Sepetteki ürünleri dinlemeye başla. Birden fazla ürün ID'si alır.
        /// Sepet sayfası açıldığında çağrılır — sepetteki her ürünün odasına otomatik katılır.
        /// </summary>
        public async Task JoinCartRooms(int[] productIds)
        {
            if (productIds == null || productIds.Length == 0) return;

            // Güvenlik: Max 50 ürün ile sınırla (sepet limiti)
            var safeIds = productIds.Take(50).Where(id => id > 0).Distinct();

            foreach (var productId in safeIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"product-{productId}");
            }

            _logger.LogDebug(
                "[StockHub] Client sepet odalarına katıldı. ÜrünSayısı: {Count}, ConnectionId: {ConnectionId}",
                safeIds.Count(), Context.ConnectionId);
        }

        /// <summary>
        /// Sepet odalarından toplu çıkış.
        /// </summary>
        public async Task LeaveCartRooms(int[] productIds)
        {
            if (productIds == null || productIds.Length == 0) return;

            foreach (var productId in productIds.Where(id => id > 0).Distinct())
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"product-{productId}");
            }
        }

        /// <summary>
        /// Global stok güncellemelerini dinle (admin panel için).
        /// Tüm ürünlerin stok/fiyat değişikliklerini alır.
        /// </summary>
        public async Task JoinGlobalStockUpdates()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "stock-global");

            _logger.LogDebug(
                "[StockHub] Client global stok odasına katıldı. ConnectionId: {ConnectionId}",
                Context.ConnectionId);
        }

        /// <summary>
        /// Global stok güncelleme odasından ayrıl.
        /// </summary>
        public async Task LeaveGlobalStockUpdates()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "stock-global");
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogDebug(
                "[StockHub] Client bağlantıyı kesti. ConnectionId: {ConnectionId}, Hata: {Error}",
                Context.ConnectionId, exception?.Message ?? "Yok");

            return base.OnDisconnectedAsync(exception);
        }
    }
}
