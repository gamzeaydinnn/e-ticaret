using System.Text;
using System.Text.Json;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Entities.Concrete;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;

namespace ECommerce.Infrastructure.Services.Micro
{
    
    public class MicroService : IMicroService
    {
        private readonly HttpClient _httpClient;

        public MicroService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        public async Task<IEnumerable<MicroProductDto>> GetProductsAsync()
        {
            var response = await _httpClient.GetAsync("erp/products");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MicroProductDto>>(json);
            throw new NotImplementedException();
    }

        public async Task<IEnumerable<MicroStockDto>> GetStocksAsync()
        {
            var response = await _httpClient.GetAsync("erp/stocks");
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<MicroStockDto>>(json);
             throw new NotImplementedException();
    }

        public async Task<bool> ExportOrdersToERPAsync(IEnumerable<Order> orders)
        {
            var json = JsonSerializer.Serialize(orders);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("erp/orders/import", content);
            return response.IsSuccessStatusCode;
            throw new NotImplementedException();
    }
    

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
