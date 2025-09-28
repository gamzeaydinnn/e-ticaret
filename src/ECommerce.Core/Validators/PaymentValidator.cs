using ECommerce.Core.DTOs.Payment; 

namespace ECommerce.Core.Validators
{
    public static class PaymentValidator
    {
        public static bool Validate(PaymentCreateDto dto, out string error)
        {
            error = null;

            if (dto.OrderId <= 0)
            {
                error = "Geçerli bir OrderId gerekli.";
                return false;
            }

            if (dto.Amount <= 0)
            {
                error = "Amount 0'dan büyük olmalı.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.PaymentMethod))
            {
                error = "PaymentMethod gerekli.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(dto.Currency))
            {
                error = "Currency gerekli.";
                return false;
            }

            return true;
        }
    }
}
 