using ECommerce.Core.DTOs.Product;
namespace ECommerce.Core.Validators
{
    public static class ProductValidator
    {
        public static bool Validate(ProductCreateDto dto, out string error)
        {
            error = null;
            if (dto is null) { error = "İstek gövdesi gerekli"; return false; }
            if (string.IsNullOrWhiteSpace(dto.Name)) { error = "Ürün adı gerekli"; return false; }
            if (dto.Price <= 0) { error = "Fiyat 0'dan büyük olmalı"; return false; }
            if (dto.StockQuantity < 0) { error = "Stok miktarı negatif olamaz"; return false; }
            if (dto.CategoryId <= 0) { error = "Geçerli bir kategori seçiniz"; return false; }
            if (dto.SpecialPrice.HasValue)
            {
                if (dto.SpecialPrice.Value <= 0) { error = "İndirimli fiyat 0'dan büyük olmalı"; return false; }
                if (dto.SpecialPrice.Value > dto.Price) { error = "İndirimli fiyat normal fiyattan büyük olamaz"; return false; }
            }
            return true;
        }
    }
}
