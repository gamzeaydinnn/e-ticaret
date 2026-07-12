namespace ECommerce.Core.DTOs.Inventory
{
    /// <summary>
    /// Sipariş kalemi stok geri yükleme satırı (yerel master: Product.StockQuantity).
    /// </summary>
    public sealed class OrderStockRestoreLineDto
    {
        public int ProductId { get; init; }
        public int? ProductVariantId { get; init; }
        public int Quantity { get; init; }
    }
}
