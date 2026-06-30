using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Interfaces
{
    /// <summary>
    /// Sipariş miktar limitlerini çözer: varyant → ürün → global önceliği.
    /// </summary>
    public interface IOrderLimitResolver
    {
        OrderLimitDto ResolveLimits(Product product, ProductVariant? variant, ProductOrderLimitSettingsDto settings);

        OrderLimitValidationResult ValidateQuantity(OrderLimitDto limits, decimal quantity);
    }

    public class OrderLimitDto
    {
        public bool IsWeightBased { get; set; }
        public string Unit { get; set; } = "adet";
        public decimal MinQuantity { get; set; }
        public decimal MaxQuantity { get; set; }
        public decimal Step { get; set; }
    }

    public class OrderLimitValidationResult
    {
        public bool IsValid { get; set; }
        public string? ErrorMessage { get; set; }
        public OrderLimitDto? Limits { get; set; }

        public static OrderLimitValidationResult Success(OrderLimitDto limits) =>
            new() { IsValid = true, Limits = limits };

        public static OrderLimitValidationResult Fail(string message, OrderLimitDto limits) =>
            new() { IsValid = false, ErrorMessage = message, Limits = limits };
    }
}
