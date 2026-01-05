using ECommerce.Core.Interfaces;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Entities.Enums;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Payment;
using Microsoft.EntityFrameworkCore;
using System;
using System.Net.Http;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace ECommerce.Infrastructure.Services.Payment
{
    /// <summary>
    /// PayTR Sanal POS Entegrasyonu
    /// https://www.paytr.com/
    /// </summary>
    public class PayTRPaymentService : IPaymentService
    {
        private readonly PaymentSettings _settings;
        private readonly ECommerceDbContext _db;
        private readonly HttpClient _httpClient;

        private const string PAYTR_API_URL = "https://www.paytr.com/odeme/api/get-token";
        private const string PAYTR_IFRAME_URL = "https://www.paytr.com/odeme/guvenli/";

        public PayTRPaymentService(IOptions<PaymentSettings> options, ECommerceDbContext db, HttpClient httpClient)
        {
            _settings = options.Value;
            _db = db;
            _httpClient = httpClient;
        }

        public virtual async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            var init = await InitiateAsync(orderId, amount, "TL");
            return init != null && init.Success && !string.IsNullOrEmpty(init.Token);
        }

        public virtual async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId)) return false;
            
            // PayTR'da ödeme durumu callback ile gelir
            // Token ile sorgulama yapılabilir
            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.ProviderPaymentId == paymentId);
            
            return payment?.Status == "Success";
        }

        public virtual Task<int> GetPaymentCountAsync() => Task.FromResult(_db.Payments.Count());

        public virtual async Task<PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount)
        {
            var ok = await ProcessPaymentAsync(orderId, amount);
            return ok ? PaymentStatus.Pending : PaymentStatus.Failed;
        }

        public virtual async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var ok = await CheckPaymentStatusAsync(paymentId);
            return ok ? PaymentStatus.Paid : PaymentStatus.Failed;
        }

        /// <summary>
        /// PayTR iframe token oluşturur
        /// </summary>
        public virtual async Task<PaymentInitResult> InitiateAsync(int orderId, decimal amount, string currency)
        {
            try
            {
                var order = await _db.Orders
                    .Include(o => o.User)
                    .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    return new PaymentInitResult
                    {
                        Success = false,
                        ErrorMessage = "Sipariş bulunamadı"
                    };
                }

                var merchantId = _settings.PayTRMerchantId;
                var merchantKey = _settings.PayTRSecretKey;
                var merchantSalt = Environment.GetEnvironmentVariable("PAYTR_MERCHANT_SALT") ?? "";
                var callbackUrl = _settings.PayTRCallbackUrl ?? "https://example.com/api/payments/paytr/callback";
                var successUrl = _settings.ReturnUrlSuccess ?? "https://example.com/order-success";
                var failUrl = _settings.ReturnUrlCancel ?? "https://example.com/order-failed";

                // Kullanıcı bilgileri
                var email = order.User?.Email ?? "customer@example.com";
                var userName = order.User?.FullName ?? "Müşteri";
                var userPhone = order.User?.PhoneNumber ?? "5551234567";
                var userAddress = order.ShippingAddress ?? "Teslimat Adresi";
                var userIp = "127.0.0.1"; // Request'ten alınmalı

                // Sepet oluştur (JSON formatında)
                var basket = new List<object[]>();
                foreach (var item in order.OrderItems)
                {
                    basket.Add(new object[]
                    {
                        item.Product?.Name ?? "Ürün",
                        item.UnitPrice.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                        item.Quantity
                    });
                }
                var basketJson = JsonConvert.SerializeObject(basket);
                var userBasket = Convert.ToBase64String(Encoding.UTF8.GetBytes(basketJson));

                // Benzersiz sipariş numarası
                var merchantOid = $"ORDER_{orderId}_{DateTime.UtcNow.Ticks}";

                // Tutar (kuruş cinsinden, örn: 100.50 TL = 10050)
                var paymentAmount = ((int)(amount * 100)).ToString();

                // Test modu (1=test, 0=production)
                var testMode = "1";
                var debugOn = "1";

                // Taksit seçenekleri (0=taksit yok)
                var noInstallment = "0";
                var maxInstallment = "0";

                // Para birimi
                var currencyCode = currency?.ToUpperInvariant() == "USD" ? "USD" : "TL";

                // Hash oluştur
                var hashStr = string.Concat(
                    merchantId,
                    userIp,
                    merchantOid,
                    email,
                    paymentAmount,
                    userBasket,
                    noInstallment,
                    maxInstallment,
                    currencyCode,
                    testMode
                );
                var paytrToken = GeneratePayTRHash(hashStr, merchantKey, merchantSalt);

                // API isteği için parametreler
                var formData = new Dictionary<string, string>
                {
                    { "merchant_id", merchantId },
                    { "user_ip", userIp },
                    { "merchant_oid", merchantOid },
                    { "email", email },
                    { "payment_amount", paymentAmount },
                    { "paytr_token", paytrToken },
                    { "user_basket", userBasket },
                    { "debug_on", debugOn },
                    { "no_installment", noInstallment },
                    { "max_installment", maxInstallment },
                    { "user_name", userName },
                    { "user_address", userAddress },
                    { "user_phone", userPhone },
                    { "merchant_ok_url", successUrl },
                    { "merchant_fail_url", failUrl },
                    { "timeout_limit", "30" },
                    { "currency", currencyCode },
                    { "test_mode", testMode }
                };

                // PayTR API'ye istek gönder
                var content = new FormUrlEncodedContent(formData);
                var response = await _httpClient.PostAsync(PAYTR_API_URL, content);
                var responseBody = await response.Content.ReadAsStringAsync();

                var responseData = JsonConvert.DeserializeObject<PayTRTokenResponse>(responseBody);

                if (responseData?.status == "success" && !string.IsNullOrEmpty(responseData.token))
                {
                    // Ödeme kaydı oluştur
                    var payment = new Payments
                    {
                        OrderId = orderId,
                        Amount = amount,
                        Provider = "PayTR",
                        ProviderPaymentId = responseData.token,
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow,
                        RawResponse = responseBody
                    };
                    _db.Payments.Add(payment);
                    await _db.SaveChangesAsync();

                    return new PaymentInitResult
                    {
                        Success = true,
                        Token = responseData.token,
                        CheckoutUrl = PAYTR_IFRAME_URL + responseData.token,
                        RedirectUrl = PAYTR_IFRAME_URL + responseData.token,
                        RequiresRedirect = true,
                        Provider = "PayTR",
                        OrderId = orderId,
                        Amount = amount
                    };
                }
                else
                {
                    return new PaymentInitResult
                    {
                        Success = false,
                        ErrorMessage = responseData?.reason ?? "PayTR token alınamadı"
                    };
                }
            }
            catch (Exception ex)
            {
                return new PaymentInitResult
                {
                    Success = false,
                    ErrorMessage = $"PayTR hatası: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// PayTR Hash oluşturur
        /// </summary>
        private string GeneratePayTRHash(string data, string merchantKey, string merchantSalt)
        {
            var hashString = data + merchantSalt;
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(merchantKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(hashString));
            return Convert.ToBase64String(hash);
        }

        /// <summary>
        /// PayTR callback doğrulama
        /// </summary>
        public bool ValidateCallback(string merchantOid, string status, string totalAmount, string hash)
        {
            var merchantKey = _settings.PayTRSecretKey;
            var merchantSalt = Environment.GetEnvironmentVariable("PAYTR_MERCHANT_SALT") ?? "";
            
            var hashStr = string.Concat(merchantOid, merchantSalt, status, totalAmount);
            var expectedHash = GeneratePayTRHash(hashStr, merchantKey, "");
            
            return string.Equals(hash, expectedHash, StringComparison.Ordinal);
        }

        /// <summary>
        /// Callback sonrası ödeme durumunu güncelle
        /// </summary>
        public async Task<bool> ProcessCallbackAsync(string merchantOid, string status, string totalAmount, string hash)
        {
            if (!ValidateCallback(merchantOid, status, totalAmount, hash))
            {
                return false;
            }

            // Order ID'yi merchantOid'den çıkar (ORDER_{id}_{ticks})
            var parts = merchantOid.Split('_');
            if (parts.Length < 2 || !int.TryParse(parts[1], out var orderId))
            {
                return false;
            }

            var payment = await _db.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Provider == "PayTR");

            if (payment == null) return false;

            if (status == "success")
            {
                payment.Status = "Success";
                payment.PaidAt = DateTime.UtcNow;

                var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
                if (order != null)
                {
                    var previousStatus = order.Status;
                    order.Status = OrderStatus.Paid;
                    
                    _db.OrderStatusHistories.Add(new OrderStatusHistory
                    {
                        OrderId = order.Id,
                        PreviousStatus = previousStatus,
                        NewStatus = OrderStatus.Paid,
                        ChangedAt = DateTime.UtcNow,
                        ChangedBy = "PayTR Callback"
                    });
                }
            }
            else
            {
                payment.Status = "Failed";
            }

            await _db.SaveChangesAsync();
            return true;
        }
    }

    /// <summary>
    /// PayTR API Token yanıtı
    /// </summary>
    public class PayTRTokenResponse
    {
        public string? status { get; set; }
        public string? token { get; set; }
        public string? reason { get; set; }
    }
}
