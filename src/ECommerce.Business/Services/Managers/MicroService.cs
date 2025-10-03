using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ECommerce.Business.Services.Managers
{
    public class MicroService : IMicroService
    {
        public async Task<IEnumerable<MicroProductDto>> GetProductsAsync()
        {
            // Mikro ERP'den ürünleri çek (örnek)
            return new List<MicroProductDto>();
        }

        public async Task<IEnumerable<MicroStockDto>> GetStocksAsync()
        {
            // Mikro ERP'den stokları çek (örnek)
            return new List<MicroStockDto>();
        }

        public async Task UpdateProduct(MicroProductDto product)
        {
            // Mikro ERP'ye ürün güncelleme işlemi
            await Task.CompletedTask;
        }

        public async Task<bool> ExportOrdersToERPAsync(IEnumerable<Order> orders)
        {
            // Siparişleri Mikro ERP'ye gönder (örnek)
            return await Task.FromResult(true);
        }

        void IMicroService.UpdateProduct(MicroProductDto productDto)
        {
            throw new NotImplementedException();
        }

        public void UpdateStock(MicroStockDto stockDto)
        {
            throw new NotImplementedException();
        }
    }
}
