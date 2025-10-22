using ECommerce.Core.Interfaces;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Entities.Enums;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Payment;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;

namespace ECommerce.Infrastructure.Services.Payment
{
    public class IyzicoPaymentService : IPaymentService
    {
        private readonly PaymentSettings _settings;
        private readonly ECommerceDbContext _db;

        public IyzicoPaymentService(IOptions<PaymentSettings> options, ECommerceDbContext db)
        {
            _settings = options.Value;
            _db = db;
        }
        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            var init = await InitiateAsync(orderId, amount, "TRY");
            return init != null;
        }

        public async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId)) return false;
            var options = new Iyzipay.Options
            {
                ApiKey = _settings.IyzicoApiKey,
                SecretKey = _settings.IyzicoSecretKey,
                BaseUrl = _settings.IyzicoBaseUrl
            };
            var req = new RetrieveCheckoutFormRequest { Token = paymentId };
            try
            {
                var result = await CheckoutForm.Retrieve(req, options);
                var success = result?.PaymentStatus == "SUCCESS" || result?.Status?.ToLowerInvariant() == "success";
                return success;
            }
            catch
            {
                return false;
            }
        }

        public Task<int> GetPaymentCountAsync() => Task.FromResult(_db.Payments.Count());

        public async Task<PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount)
        {
            var ok = await ProcessPaymentAsync(orderId, amount);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var ok = await CheckPaymentStatusAsync(paymentId);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public async Task<PaymentInitResult> InitiateAsync(int orderId, decimal amount, string currency)
        {
            var options = new Iyzipay.Options
            {
                ApiKey = _settings.IyzicoApiKey,
                SecretKey = _settings.IyzicoSecretKey,
                BaseUrl = _settings.IyzicoBaseUrl
            };

            var basketId = orderId.ToString();
            var callback = _settings.IyzicoCallbackUrl ?? "https://example.com/api/payments/iyzico/callback";

            var request = new CreateCheckoutFormInitializeRequest
            {
                Locale = Locale.TR.ToString(),
                ConversationId = basketId,
                Price = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                PaidPrice = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                Currency = currency?.ToUpperInvariant() == "USD" ? Currency.USD.ToString() : Currency.TRY.ToString(),
                BasketId = basketId,
                PaymentGroup = PaymentGroup.PRODUCT.ToString(),
                CallbackUrl = callback
            };

            // Buyer / adres zorunlu alanlarını minimal dolduruyoruz
            request.Buyer = new Buyer
            {
                Id = (orderId).ToString(),
                Name = "Musteri",
                Surname = "",
                Email = "customer@example.com",
                IdentityNumber = "11111111111",
                RegistrationAddress = "-",
                Ip = "127.0.0.1",
                City = "-",
                Country = "TR"
            };
            request.ShippingAddress = new Iyzipay.Model.Address { ContactName = "Musteri", City = "-", Country = "TR", Description = "-" };
            request.BillingAddress = new Iyzipay.Model.Address { ContactName = "Musteri", City = "-", Country = "TR", Description = "-" };

            request.BasketItems = new List<BasketItem>
            {
                new BasketItem
                {
                    Id = basketId,
                    Name = $"Order #{orderId}",
                    Category1 = "Order",
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)
                }
            };

            var initialize = await CheckoutFormInitialize.Create(request, options);

            // Kayıt
            var pay = new Payments
            {
                OrderId = orderId,
                Provider = "iyzico",
                ProviderPaymentId = initialize.Token,
                Amount = amount,
                Status = "Pending",
                RawResponse = initialize.CheckoutFormContent
            };
            _db.Payments.Add(pay);
            await _db.SaveChangesAsync();

            return new PaymentInitResult
            {
                Provider = "iyzico",
                RequiresRedirect = true,
                RedirectUrl = initialize.PaymentPageUrl,
                ProviderPaymentId = initialize.Token,
                Currency = (currency ?? "TRY").ToUpperInvariant(),
                Amount = amount,
                OrderId = orderId
            };
        }
    }
}
// Not: Iyzipay için demo/sandbox API anahtarları kullanılmalıdır. Buyer/address zorunlu alanları gerçek kurulumda siparişten doldurulmalıdır.
