using ECommerce.Core.Interfaces;
using ECommerce.Infrastructure.Config;
using Microsoft.Extensions.Options;
using ECommerce.Entities.Enums;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.DTOs.Payment;
using Stripe;
using Stripe.Checkout;

namespace ECommerce.Infrastructure.Services.Payment
{
    public class StripePaymentService : IPaymentService
    {
        private readonly PaymentSettings _settings;
        private readonly ECommerceDbContext _db;

        public StripePaymentService(IOptions<PaymentSettings> options, ECommerceDbContext db)
        {
            _settings = options.Value;
            _db = db;
            if (!string.IsNullOrWhiteSpace(_settings.StripeSecretKey))
            {
                StripeConfiguration.ApiKey = _settings.StripeSecretKey;
            }
        }
        public virtual async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            var init = await InitiateAsync(orderId, amount, "TRY");
            return init != null;
        }

        public virtual async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            if (string.IsNullOrWhiteSpace(paymentId)) return false;
            var sessionService = new SessionService();
            try
            {
                var session = await sessionService.GetAsync(paymentId);
                return session.PaymentStatus == "paid";
            }
            catch
            {
                // fallback: PaymentIntent ID olabilir
                try
                {
                    var piService = new PaymentIntentService();
                    var pi = await piService.GetAsync(paymentId);
                    return pi.Status == "succeeded";
                }
                catch { return false; }
            }
        }

        public virtual Task<int> GetPaymentCountAsync() => Task.FromResult(_db.Payments.Count());

        public virtual async Task<PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount)
        {
            var ok = await ProcessPaymentAsync(orderId, amount);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public virtual async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var ok = await CheckPaymentStatusAsync(paymentId);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public virtual async Task<PaymentInitResult> InitiateAsync(int orderId, decimal amount, string currency)
        {
            // Create checkout session
            var successUrl = _settings.ReturnUrlSuccess ?? "https://example.com/success";
            var cancelUrl = _settings.ReturnUrlCancel ?? "https://example.com/cancel";

            var options = new SessionCreateOptions
            {
                Mode = "payment",
                SuccessUrl = successUrl + (successUrl.Contains("?") ? "&" : "?") + $"session_id={{CHECKOUT_SESSION_ID}}&orderId={orderId}",
                CancelUrl = cancelUrl + (cancelUrl.Contains("?") ? "&" : "?") + $"orderId={orderId}",
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = new List<SessionLineItemOptions>
                {
                    new SessionLineItemOptions
                    {
                        Quantity = 1,
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            Currency = (currency ?? "TRY").ToLowerInvariant(),
                            UnitAmount = (long)(amount * 100),
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = $"Order #{orderId}"
                            }
                        }
                    }
                },
                Metadata = new Dictionary<string, string> { { "orderId", orderId.ToString() } },
                ClientReferenceId = orderId.ToString()
            };

            var sessionService = new SessionService();
            var session = await sessionService.CreateAsync(options);

            // Payment kaydı
            var pay = new Payments
            {
                OrderId = orderId,
                Provider = "stripe",
                ProviderPaymentId = session.Id,
                Amount = amount,
                Status = "Pending",
                RawResponse = session.Id
            };
            _db.Payments.Add(pay);
            await _db.SaveChangesAsync();

            return new PaymentInitResult
            {
                Provider = "stripe",
                RequiresRedirect = true,
                RedirectUrl = session.Url,
                CheckoutSessionId = session.Id,
                ClientSecret = session.ClientSecret,
                Currency = (currency ?? "TRY").ToUpperInvariant(),
                Amount = amount,
                OrderId = orderId,
                ProviderPaymentId = session.Id
            };
        }
    }
}
