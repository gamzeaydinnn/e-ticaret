using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services.Micro
{
    public class MicroService : IMicroService
    {
        public void UpdateProduct(MicroProductDto productDto)
        {
            // Mikro ERP API çağrısı yap
        }

        public void UpdateStock(MicroStockDto stockDto)
        {
            // Mikro ERP stok güncellemesi
        }
    }
}
