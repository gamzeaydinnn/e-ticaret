using System.Text.RegularExpressions;
using ECommerce.Entities.Enums;

namespace ECommerce.Business.Helpers
{
    public static class WeightBasedProductRules
    {
        private static readonly Regex FixedWeightPattern =
            new(@"\b\d+(?:[.,]\d+)?\s*(GR|KG|LT|ML|CL|L)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex StandaloneKgPattern =
            new(@"\bKG\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static bool IsVariableWeightKgProduct(
            string? productName,
            WeightUnit weightUnit,
            string? categoryNameOrCode)
        {
            if (weightUnit != WeightUnit.Kilogram)
            {
                return false;
            }

            var normalizedName = (productName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                return false;
            }

            if (FixedWeightPattern.IsMatch(normalizedName))
            {
                return false;
            }

            if (!StandaloneKgPattern.IsMatch(normalizedName))
            {
                return false;
            }

            return true;
        }
    }
}
