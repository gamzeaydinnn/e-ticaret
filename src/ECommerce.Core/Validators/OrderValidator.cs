using ECommerce.Core.DTOs.Order;
namespace ECommerce.Core.Validators
{
    public static class OrderValidator
    {
        public static bool Validate(OrderCreateDto dto, out string error)
        {
            error = null;
            if (dto is null) { error = "İstek gövdesi gerekli"; return false; }
            if (dto.UserId <= 0) { error = "Geçerli bir kullanıcı gerekli"; return false; }
            if (dto.TotalPrice <= 0) { error = "Toplam tutar 0'dan büyük olmalı"; return false; }
            if (dto.OrderItems == null || dto.OrderItems.Count == 0) { error = "Sipariş öğesi bulunmuyor"; return false; }

            foreach (var item in dto.OrderItems)
            {
                if (item.ProductId <= 0) { error = "Sipariş öğelerinde geçersiz ürün bilgisi var"; return false; }
                if (item.Quantity <= 0) { error = "Sipariş öğelerinde miktar 0'dan büyük olmalı"; return false; }
                if (item.UnitPrice <= 0) { error = "Sipariş öğelerinde birim fiyat 0'dan büyük olmalı"; return false; }
            }

            return true;
        }
    }
}
