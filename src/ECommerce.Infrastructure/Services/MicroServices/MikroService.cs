using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services.Mikro
{
    public class MikroService : IMicroService
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
