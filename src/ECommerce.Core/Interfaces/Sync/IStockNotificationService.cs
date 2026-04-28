namespace ECommerce.Core.Interfaces.Sync
{
    /// <summary>
    /// Stok/Fiyat/Ürün değişikliklerini SignalR üzerinden frontend'e ileten bildirim servisi.
    /// 
    /// NEDEN: Ürün detay sayfasındaki kullanıcı stok tükendiğini anlık görmeli,
    /// sepetteki ürünün fiyatı değiştiğinde uyarılmalı. SSE veya HTTP polling yerine
    /// SignalR tercih edildi çünkü projede zaten 5 hub aktif kullanılıyor.
    /// 
    /// STRATEJİ:
    /// - Tekil değişiklikler: product-{id} grubuna gönderilir
    /// - Toplu değişiklikler: "stock-updates" global grubuna batch olarak gönderilir
    /// - Admin bildirimleri: admin-notifications grubuna ayrıca gönderilir
    /// </summary>
    public interface IStockNotificationService
    {
        /// <summary>
        /// Tek bir ürünün stok değişikliğini bildirir.
        /// Hem ürün detay sayfası hem de sepet sayfası dinler.
        /// </summary>
        Task NotifyStockChangedAsync(
            int productId,
            string productName,
            int oldQuantity,
            int newQuantity,
            string source,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Tek bir ürünün fiyat değişikliğini bildirir.
        /// </summary>
        Task NotifyPriceChangedAsync(
            int productId,
            string productName,
            decimal oldPrice,
            decimal newPrice,
            string source,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Ürün bilgi değişikliğini bildirir (ad, birim vb.).
        /// </summary>
        Task NotifyProductInfoChangedAsync(
            int productId,
            string oldName,
            string newName,
            string source,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Toplu stok güncelleme bildirimi (HotPoll/UnifiedSync sonrası).
        /// Frontend batch olarak işler — her biri için ayrı event göndermez.
        /// </summary>
        Task NotifyBulkStockUpdateAsync(
            IEnumerable<ProductChangeEvent> changes,
            string source,
            CancellationToken cancellationToken = default);
    }
}
