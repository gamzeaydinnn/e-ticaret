namespace ECommerce.Core.DTOs.Product
{
    public sealed class AdminCatalogProductSnapshot
    {
        public int Id { get; init; }
        public string Sku { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int StockQuantity { get; init; }
        public bool IsActive { get; init; }
        public int? CategoryId { get; init; }
    }
}
