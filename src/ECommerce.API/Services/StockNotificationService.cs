using ECommerce.API.Hubs;
using ECommerce.Core.Interfaces.Sync;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Services
{
    /// <summary>
    /// Stok/Fiyat/Ürün değişikliklerini StockHub üzerinden frontend'e push eden servis.
    /// 
    /// NEDEN: HotPoll veya Outbound Push tespit ettiği değişiklikleri frontend'e
    /// iletmek için merkezi bir nokta gerekiyor. Bu servis IHubContext kullanarak
    /// doğru gruplara doğru mesajları gönderir.
    /// 
    /// TASARIM KARARI: Mevcut RealTimeNotificationService'e eklenmedi çünkü
    /// stok bildirimleri yüksek frekanslı (10sn'de bir potansiyel) ve farklı
    /// grup yapısına sahip. Separation of Concerns.
    /// </summary>
    public class StockNotificationService : IStockNotificationService
    {
        private readonly IHubContext<StockHub> _stockHub;
        private readonly IHubContext<AdminNotificationHub> _adminHub;
        private readonly ILogger<StockNotificationService> _logger;

        // Toplu bildirim gönderiminde batch boyutu — bir seferde çok fazla mesaj gönderip
        // SignalR buffer'ını taşırmamak için sınır.
        private const int MaxBulkBatchSize = 100;

        public StockNotificationService(
            IHubContext<StockHub> stockHub,
            IHubContext<AdminNotificationHub> adminHub,
            ILogger<StockNotificationService> logger)
        {
            _stockHub = stockHub ?? throw new ArgumentNullException(nameof(stockHub));
            _adminHub = adminHub ?? throw new ArgumentNullException(nameof(adminHub));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public async Task NotifyStockChangedAsync(
            int productId,
            string productName,
            int oldQuantity,
            int newQuantity,
            string source,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Ürünü izleyen kullanıcılara bildir (ürün detay + sepet)
                await _stockHub.Clients
                    .Group($"product-{productId}")
                    .SendAsync("StockChanged", new
                    {
                        productId,
                        productName,
                        oldQuantity,
                        newQuantity,
                        source,
                        timestamp = DateTime.UtcNow,
                        isOutOfStock = newQuantity <= 0
                    }, cancellationToken);

                // 2. Global stok izleyicilerine bildir (admin panel)
                await _stockHub.Clients
                    .Group("stock-global")
                    .SendAsync("StockChanged", new
                    {
                        productId,
                        productName,
                        oldQuantity,
                        newQuantity,
                        source,
                        timestamp = DateTime.UtcNow,
                        isOutOfStock = newQuantity <= 0
                    }, cancellationToken);

                // 3. Stok sıfıra düştüyse admin'e özel alert
                if (newQuantity <= 0 && oldQuantity > 0)
                {
                    await _adminHub.Clients
                        .Group("admin-notifications")
                        .SendAsync("StockAlert", new
                        {
                            type = "out_of_stock",
                            productId,
                            productName,
                            previousStock = oldQuantity,
                            source,
                            timestamp = DateTime.UtcNow
                        }, cancellationToken);
                }

                _logger.LogDebug(
                    "[StockNotification] Stok bildirimi gönderildi. " +
                    "ProductId: {ProductId}, {Old}→{New}, Kaynak: {Source}",
                    productId, oldQuantity, newQuantity, source);
            }
            catch (Exception ex)
            {
                // SignalR hatası senkronizasyonu engellemez — fire-and-forget mantığı
                _logger.LogWarning(ex,
                    "[StockNotification] Stok bildirimi gönderilemedi. ProductId: {ProductId}",
                    productId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyPriceChangedAsync(
            int productId,
            string productName,
            decimal oldPrice,
            decimal newPrice,
            string source,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _stockHub.Clients
                    .Group($"product-{productId}")
                    .SendAsync("PriceChanged", new
                    {
                        productId,
                        productName,
                        oldPrice,
                        newPrice,
                        source,
                        timestamp = DateTime.UtcNow,
                        differencePercent = oldPrice > 0
                            ? Math.Round((newPrice - oldPrice) / oldPrice * 100, 2)
                            : 0m
                    }, cancellationToken);

                await _stockHub.Clients
                    .Group("stock-global")
                    .SendAsync("PriceChanged", new
                    {
                        productId,
                        productName,
                        oldPrice,
                        newPrice,
                        source,
                        timestamp = DateTime.UtcNow
                    }, cancellationToken);

                _logger.LogDebug(
                    "[StockNotification] Fiyat bildirimi gönderildi. " +
                    "ProductId: {ProductId}, {Old}→{New}, Kaynak: {Source}",
                    productId, oldPrice, newPrice, source);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[StockNotification] Fiyat bildirimi gönderilemedi. ProductId: {ProductId}",
                    productId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyProductInfoChangedAsync(
            int productId,
            string oldName,
            string newName,
            string source,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await _stockHub.Clients
                    .Group($"product-{productId}")
                    .SendAsync("ProductInfoChanged", new
                    {
                        productId,
                        oldName,
                        newName,
                        source,
                        timestamp = DateTime.UtcNow
                    }, cancellationToken);

                _logger.LogDebug(
                    "[StockNotification] Ürün bilgi bildirimi gönderildi. ProductId: {ProductId}",
                    productId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[StockNotification] Ürün bilgi bildirimi gönderilemedi. ProductId: {ProductId}",
                    productId);
            }
        }

        /// <inheritdoc />
        public async Task NotifyBulkStockUpdateAsync(
            IEnumerable<ProductChangeEvent> changes,
            string source,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var changeList = changes.ToList();
                if (changeList.Count == 0) return;

                // Batch'ler halinde gönder — SignalR message buffer taşmasını önler
                foreach (var batch in changeList.Chunk(MaxBulkBatchSize))
                {
                    var payload = batch.Select(c => new
                    {
                        productId = c.LocalProductId,
                        stokKod = c.StokKod,
                        changeType = c.ChangeType.ToString(),
                        oldStock = c.OldStockQuantity,
                        newStock = c.NewStockQuantity,
                        oldPrice = c.OldPrice,
                        newPrice = c.NewPrice
                    }).ToArray();

                    // Global gruba toplu bildirim
                    await _stockHub.Clients
                        .Group("stock-global")
                        .SendAsync("BulkStockUpdate", new
                        {
                            updates = payload,
                            source,
                            totalCount = changeList.Count,
                            timestamp = DateTime.UtcNow
                        }, cancellationToken);

                    // Her değişen ürünün kendi grubuna da bildirim
                    foreach (var change in batch.Where(c => c.LocalProductId.HasValue))
                    {
                        if (change.ChangeType.HasFlag(ProductChangeType.Stock))
                        {
                            await _stockHub.Clients
                                .Group($"product-{change.LocalProductId}")
                                .SendAsync("StockChanged", new
                                {
                                    productId = change.LocalProductId,
                                    oldQuantity = (int)(change.OldStockQuantity ?? 0),
                                    newQuantity = (int)(change.NewStockQuantity ?? 0),
                                    source,
                                    timestamp = DateTime.UtcNow,
                                    isOutOfStock = (change.NewStockQuantity ?? 0) <= 0
                                }, cancellationToken);
                        }

                        if (change.ChangeType.HasFlag(ProductChangeType.Price))
                        {
                            await _stockHub.Clients
                                .Group($"product-{change.LocalProductId}")
                                .SendAsync("PriceChanged", new
                                {
                                    productId = change.LocalProductId,
                                    oldPrice = change.OldPrice ?? 0,
                                    newPrice = change.NewPrice ?? 0,
                                    source,
                                    timestamp = DateTime.UtcNow
                                }, cancellationToken);
                        }
                    }
                }

                _logger.LogInformation(
                    "[StockNotification] Toplu stok bildirimi gönderildi. " +
                    "Değişen: {Count}, Kaynak: {Source}",
                    changeList.Count, source);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[StockNotification] Toplu stok bildirimi gönderilemedi.");
            }
        }
    }
}
