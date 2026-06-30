using ECommerce.Business.Helpers;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Helpers
{
    /// <summary>
    /// Sipariş miktar limitlerini tek noktadan çözer.
    /// Öncelik: varyant → ürün → global varsayılan.
    /// </summary>
    public class OrderLimitResolver : IOrderLimitResolver
    {
        public OrderLimitDto ResolveLimits(
            Product product,
            ProductVariant? variant,
            ProductOrderLimitSettingsDto settings)
        {
            if (product == null)
            {
                throw new ArgumentNullException(nameof(product));
            }

            settings ??= new ProductOrderLimitSettingsDto();
            var isWeightBased = WeightBasedProductResolver.ResolveIsWeightBased(product);

            if (isWeightBased)
            {
                var maxKg = product.MaxOrderWeight > 0m
                    ? product.MaxOrderWeight / 1000m
                    : settings.DefaultMaxWeightKg;

                var minKg = product.MinOrderWeight > 0m
                    ? product.MinOrderWeight / 1000m
                    : settings.DefaultMinWeightKg;

                return new OrderLimitDto
                {
                    IsWeightBased = true,
                    Unit = "kg",
                    MinQuantity = minKg,
                    MaxQuantity = maxKg,
                    Step = settings.DefaultWeightStepKg
                };
            }

            var maxQuantity = variant?.MaxOrderQuantity > 0
                ? variant.MaxOrderQuantity
                : product.MaxOrderQuantity > 0
                    ? product.MaxOrderQuantity
                    : settings.DefaultMaxQuantityPiece;

            var minQuantity = product.MinOrderQuantity > 0
                ? product.MinOrderQuantity
                : settings.DefaultMinQuantityPiece;

            var step = product.QuantityStep > 0m
                ? product.QuantityStep
                : settings.DefaultQuantityStepPiece;

            return new OrderLimitDto
            {
                IsWeightBased = false,
                Unit = "adet",
                MinQuantity = minQuantity,
                MaxQuantity = maxQuantity,
                Step = step
            };
        }

        public OrderLimitValidationResult ValidateQuantity(OrderLimitDto limits, decimal quantity)
        {
            if (limits == null)
            {
                throw new ArgumentNullException(nameof(limits));
            }

            if (quantity < limits.MinQuantity)
            {
                return OrderLimitValidationResult.Fail(
                    $"Minimum sipariş miktarı {limits.MinQuantity} {limits.Unit}.",
                    limits);
            }

            if (quantity > limits.MaxQuantity)
            {
                return OrderLimitValidationResult.Fail(
                    $"Maksimum sipariş miktarı {limits.MaxQuantity} {limits.Unit}.",
                    limits);
            }

            return OrderLimitValidationResult.Success(limits);
        }
    }
}
