using ECommerce.Core.Interfaces;
using ECommerce.Entities.Enums;
using System;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Payment;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.Payment;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace ECommerce.Business.Services.Managers
{
    public class PaymentManager : IPaymentService
    {
        private readonly StripePaymentService _stripe;
        private readonly IyzicoPaymentService _iyzico;
        private readonly PayPalPaymentService _paypal;
        private readonly ECommerceDbContext _db;
        private readonly ILogService _logService;
        private readonly string _defaultProvider;

        public PaymentManager(
            StripePaymentService stripe,
            IyzicoPaymentService iyzico,
            PayPalPaymentService paypal,
            ECommerceDbContext db,
            ILogService logService,
            IConfiguration configuration)
        {
            _stripe = stripe;
            _iyzico = iyzico;
            _paypal = paypal;
            _db = db;
            _logService = logService;
            _defaultProvider = configuration["Payment:Provider"]?.ToLowerInvariant() ?? "stripe";
        }

        private (IPaymentService service, string providerKey) ResolveProviderByMethod(string? method)
        {
            var key = (method ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(key))
            {
                return ResolveDefaultProvider();
            }

            return key switch
            {
                "stripe" => (_stripe, "stripe"),
                "paypal" => (_paypal, "paypal"),
                "iyzico" or "iyzipay" => (_iyzico, "iyzico"),
                // Eski arayüzde creditCard seçimi genelde kartlı sağlayıcıya gider
                "creditcard" or "credit_card" => ResolveDefaultProvider(),
                _ => ResolveDefaultProvider()
            };
        }

        private (IPaymentService service, string providerKey) ResolveDefaultProvider()
        {
            var key = _defaultProvider;
            return key switch
            {
                "iyzico" or "iyzipay" => (_iyzico, "iyzico"),
                "paypal" => (_paypal, "paypal"),
                _ => (_stripe, "stripe")
            };
        }

        private async Task<(IPaymentService service, string providerKey)> ResolveProviderByPaymentIdAsync(string paymentId)
        {
            if (!string.IsNullOrWhiteSpace(paymentId))
            {
                var existing = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == paymentId);
                if (existing != null)
                {
                    return ResolveProviderByMethod(existing.Provider);
                }
            }

            return ResolveDefaultProvider();
        }

        private async Task LogFailureAsync(
            int orderId,
            decimal amount,
            string providerKey,
            string stage,
            string message,
            Exception? ex = null,
            string? providerPaymentId = null)
        {
            try
            {
                var payment = new Payments
                {
                    OrderId = orderId,
                    Provider = providerKey,
                    ProviderPaymentId = providerPaymentId ?? $"FAILED-{Guid.NewGuid():N}",
                    Amount = amount,
                    Status = "Failed",
                    RawResponse = message
                };

                _db.Payments.Add(payment);
                await _db.SaveChangesAsync();

                var ctx = new Dictionary<string, object>
                {
                    { "orderId", orderId },
                    { "amount", amount },
                    { "provider", providerKey },
                    { "stage", stage },
                    { "message", message },
                    { "providerPaymentId", payment.ProviderPaymentId }
                };

                if (ex != null)
                {
                    _logService.Error(ex, "Ödeme başarısız", ctx);
                }
                else
                {
                    _logService.Warn("Ödeme başarısız", ctx);
                }

                _logService.Audit(
                    action: "PAYMENT_FAILED",
                    entityName: "Payments",
                    entityId: payment.Id,
                    oldValues: null,
                    newValues: new
                    {
                        orderId,
                        amount,
                        provider = providerKey,
                        stage,
                        message
                    },
                    performedBy: null);
            }
            catch
            {
                // Loglama / audit işlemi ana akışı bozmamalı
            }
        }

        public async Task<bool> ProcessPaymentAsync(int orderId, decimal amount)
        {
            var (service, providerKey) = ResolveDefaultProvider();
            try
            {
                var ok = await service.ProcessPaymentAsync(orderId, amount);
                if (!ok)
                {
                    await LogFailureAsync(orderId, amount, providerKey, "PROCESS", "ProcessPaymentAsync false döndü");
                }

                return ok;
            }
            catch (Exception ex)
            {
                await LogFailureAsync(orderId, amount, providerKey, "PROCESS_EXCEPTION", ex.Message, ex);
                throw;
            }
        }

        public async Task<bool> CheckPaymentStatusAsync(string paymentId)
        {
            var (service, _) = await ResolveProviderByPaymentIdAsync(paymentId);
            return await service.CheckPaymentStatusAsync(paymentId);
        }

        public async Task<int> GetPaymentCountAsync()
        {
            return await _db.Payments.CountAsync();
        }

        public async Task<PaymentStatus> ProcessPaymentDetailedAsync(int orderId, decimal amount)
        {
            var ok = await ProcessPaymentAsync(orderId, amount);
            return ok ? PaymentStatus.Successful : PaymentStatus.Failed;
        }

        public async Task<PaymentStatus> GetPaymentStatusAsync(string paymentId)
        {
            var (service, _) = await ResolveProviderByPaymentIdAsync(paymentId);
            return await service.GetPaymentStatusAsync(paymentId);
        }

        public async Task<PaymentInitResult> InitiateAsync(int orderId, decimal amount, string currency)
        {
            var (service, _) = ResolveDefaultProvider();
            return await service.InitiateAsync(orderId, amount, currency);
        }

        /// <summary>
        /// PaymentCreateDto içindeki PaymentMethod alanına göre sağlayıcı seçerek
        /// hosted checkout / 3D Secure akışını başlatır.
        /// </summary>
        public async Task<PaymentInitResult> InitiateAsync(PaymentCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var (service, providerKey) = ResolveProviderByMethod(dto.PaymentMethod);

            try
            {
                var result = await service.InitiateAsync(dto.OrderId, dto.Amount, dto.Currency ?? "TRY");
                return result;
            }
            catch (Exception ex)
            {
                await LogFailureAsync(dto.OrderId, dto.Amount, providerKey, "INITIATE_EXCEPTION", ex.Message, ex);
                throw;
            }
        }
    }
}
