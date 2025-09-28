using ECommerce.Core.DTOs.Cart;

namespace ECommerce.Core.Validators
{
    public static class CartValidator
    {
        public static bool Validate(CartItemDto dto, out string error)
        {
            error = null;

            if (dto.ProductId <= 0)
            {
                error = "Geçerli bir ProductId gerekli.";
                return false;
            }

            if (dto.Quantity <= 0)
            {
                error = "Quantity 0'dan büyük olmalı.";
                return false;
            }

            return true;
        }
    }
}
