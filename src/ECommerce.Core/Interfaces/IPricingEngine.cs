using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Pricing;

namespace ECommerce.Core.Interfaces
{
    public interface IPricingEngine
    {
        Task<CartPricingResultDto> CalculateCartAsync(
            int? userId,
            IEnumerable<CartItemInputDto> items,
            string? couponCode);
    }
}

