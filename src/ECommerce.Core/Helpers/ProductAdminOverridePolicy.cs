using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Core.Helpers
{
    public static class ProductAdminOverridePolicy
    {
        public static bool ResolveOverride(bool? productOverride, bool globalDefault)
        {
            return productOverride ?? globalDefault;
        }

        public static bool ShouldUseAdminName(Product? product, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return ResolveOverride(product?.AdminOverrideName, defaults?.DefaultAdminOverrideName ?? false)
                && !string.IsNullOrWhiteSpace(product?.Name);
        }

        public static bool ShouldUseAdminPrice(Product? product, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return ResolveOverride(product?.AdminOverridePrice, defaults?.DefaultAdminOverridePrice ?? false)
                && product?.Price > 0;
        }

        public static bool ShouldUseAdminCategory(Product? product, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return ResolveOverride(product?.AdminOverrideCategory, defaults?.DefaultAdminOverrideCategory ?? false)
                && product?.CategoryId > 0;
        }

        public static bool CanSyncName(Product? product, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return !ResolveOverride(product?.AdminOverrideName, defaults?.DefaultAdminOverrideName ?? false);
        }

        public static bool CanSyncPrice(Product? product, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return !ResolveOverride(product?.AdminOverridePrice, defaults?.DefaultAdminOverridePrice ?? false);
        }

        public static bool CanSyncCategory(Product? product, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return !ResolveOverride(product?.AdminOverrideCategory, defaults?.DefaultAdminOverrideCategory ?? false);
        }

        public static bool CanSyncActiveState(Product? product)
        {
            return product?.AdminDeactivated != true;
        }

        public static string ResolveName(string mikroName, Product? localProduct, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return ShouldUseAdminName(localProduct, defaults)
                ? localProduct!.Name
                : mikroName;
        }

        public static decimal ResolvePrice(decimal mikroPrice, Product? localProduct, ProductAdminOverrideSettingsDto? defaults = null)
        {
            if (ShouldUseAdminPrice(localProduct, defaults))
            {
                return localProduct!.Price;
            }

            if (mikroPrice > 0)
            {
                return mikroPrice;
            }

            if (localProduct?.Price > 0)
            {
                return localProduct.Price;
            }

            return 0m;
        }

        public static decimal? ResolveSpecialPrice(Product? localProduct, ProductAdminOverrideSettingsDto? defaults = null)
        {
            return ShouldUseAdminPrice(localProduct, defaults) ? localProduct?.SpecialPrice : null;
        }
    }
}