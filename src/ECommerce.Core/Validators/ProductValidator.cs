using ECommerce.Core.DTOs.Product;
namespace ECommerce.Core.Validators
{
    public static class ProductValidator
    {
        public static bool Validate(ProductCreateDto dto, out string error)
        {
            error = null;
            if (string.IsNullOrWhiteSpace(dto.Name)) { error = "Ürün adı gerekli"; return false; }
            if (dto.Price <= 0) { error = "Fiyat 0'dan büyük olmalı"; return false; }
            return true;
        }
    }
}
