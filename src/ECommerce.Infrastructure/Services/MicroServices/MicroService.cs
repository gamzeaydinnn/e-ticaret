using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using ECommerce.Core.DTOs.Micro;
// using ECommerce.Core.Entities.Concrete; // removed: entities live in ECommerce.Entities.Concrete
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using System;
using System.Net.Http.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Infrastructure.Services.MicroServices
{
    
    public class MicroService : IMicroService
    {
        private readonly HttpClient _httpClient;
        private readonly MikroSettings _settings;

        public MicroService(HttpClient httpClient, IOptions<MikroSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                // Basic header; signature başlıkları her istekte oluşturulacak
                _httpClient.DefaultRequestHeaders.Remove("X-API-Key");
                _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
            }
        }
        public async Task<IEnumerable<MicroProductDto>> GetProductsAsync()
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Get, "erp/products"));
            var data = await response.Content.ReadFromJsonAsync<List<MicroProductDto>>();
            return data ?? new List<MicroProductDto>();
    }

        public async Task<IEnumerable<MicroStockDto>> GetStocksAsync()
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Get, "erp/stocks"));
            var data = await response.Content.ReadFromJsonAsync<List<MicroStockDto>>();
            return data ?? new List<MicroStockDto>();
    }

        public async Task<bool> ExportOrdersToERPAsync(IEnumerable<Order> orders)
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Post, "erp/orders/import", orders));
            return response.IsSuccessStatusCode;
    }
    

        public void UpdateProduct(MicroProductDto productDto)
        {
            // Tekil ürün güncelleme (fire-and-forget değil, task dönmek daha iyi ama interface void)
            _ = SendWithRetryAsync(() => BuildRequest(HttpMethod.Post, "erp/products/update", productDto));
        }

        public void UpdateStock(MicroStockDto stockDto)
        {
            _ = SendWithRetryAsync(() => BuildRequest(HttpMethod.Post, "erp/stocks/update", stockDto));
        }
        public async Task<IEnumerable<MicroPriceDto>> GetPricesAsync()
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Get, "erp/prices"));
            var data = await response.Content.ReadFromJsonAsync<List<MicroPriceDto>>();
            return data ?? new List<MicroPriceDto>();
        }

        public async Task<IEnumerable<MicroCustomerDto>> GetCustomersAsync()
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Get, "erp/customers"));
            var data = await response.Content.ReadFromJsonAsync<List<MicroCustomerDto>>();
            return data ?? new List<MicroCustomerDto>();
        }

        public async Task<bool> UpsertProductsAsync(IEnumerable<MicroProductDto> products)
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Post, "erp/products/upsert", products));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpsertStocksAsync(IEnumerable<MicroStockDto> stocks)
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Post, "erp/stocks/upsert", stocks));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpsertPricesAsync(IEnumerable<MicroPriceDto> prices)
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Post, "erp/prices/upsert", prices));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpsertCustomersAsync(IEnumerable<MicroCustomerDto> customers)
        {
            var response = await SendWithRetryAsync(() => BuildRequest(HttpMethod.Post, "erp/customers/upsert", customers));
            return response.IsSuccessStatusCode;
        }

        private async Task<HttpResponseMessage> SendWithRetryAsync(Func<HttpRequestMessage> requestFactory, int maxAttempts = 3)
        {
            Exception? lastEx = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    using var req = requestFactory();
                    var res = await _httpClient.SendAsync(req);
                    if ((int)res.StatusCode >= 500 && attempt < maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt * attempt));
                        continue;
                    }
                    res.EnsureSuccessStatusCode();
                    return res;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(250 * attempt * attempt));
                        continue;
                    }
                    throw;
                }
            }
            throw lastEx ?? new Exception("Unknown error calling Mikro ERP");
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string path, object? body = null)
        {
            var req = new HttpRequestMessage(method, path);
            if (body != null)
            {
                var json = JsonSerializer.Serialize(body);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
                // Idempotency-Key header (özellikle POST/PUT)
                req.Headers.TryAddWithoutValidation("Idempotency-Key", Guid.NewGuid().ToString());
            }

            // İmza başlıkları
            var ts = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            req.Headers.Remove("X-API-Key");
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
                req.Headers.TryAddWithoutValidation("X-API-Key", _settings.ApiKey);
            req.Headers.TryAddWithoutValidation("X-API-Timestamp", ts);

            var toSign = method.Method + "\n" + path + "\n" + ts + "\n" + (body != null ? JsonSerializer.Serialize(body) : "");
            var sig = Sign(toSign, _settings.ApiSecret ?? string.Empty);
            req.Headers.TryAddWithoutValidation("X-API-Signature", sig);
            return req;
        }

        private static string Sign(string data, string secret)
        {
            if (string.IsNullOrEmpty(secret)) return string.Empty;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash);
        }

//ERP tarafı XML/Excel bekliyorsa, System.Xml veya ClosedXML kullanıp serialize edip gönderebilirsin.
    }//\t○ MicroService.cs — Mikro ERP ile iletişimi sağlayan adaptör (HTTP client + XML/Excel exporter/importer).

}
