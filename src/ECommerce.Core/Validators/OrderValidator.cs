using ECommerce.Core.DTOs.Order;
namespace ECommerce.Core.Validators
{
    public static class OrderValidator
    {
        public static bool Validate(OrderCreateDto dto, out string error)
        {
            error = null;
            if (dto.OrderItems == null || dto.OrderItems.Count == 0) { error = "Sipariş öğesi bulunmuyor"; return false; }
            return true;
        }
    }
}
