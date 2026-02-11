// ═══════════════════════════════════════════════════════════════════════════════════════════════
// PAYMENT MANAGER - Merkezi Ödeme Yönetim Servisi
// Tüm ödeme sağlayıcılarını (Stripe, Iyzico, PayPal, POSNET) yöneten ana servis
// ═══════════════════════════════════════════════════════════════════════════════════════════════
// NEDEN BU YAPIYI SEÇTİK?
// 1. Single Responsibility - Her provider kendi işini yapar, manager sadece yönlendirir
// 2. Open/Closed Principle - Yeni provider eklemek için manager'ı değiştirmek gerekmiyor
// 3. Strategy Pattern - Provider seçimi runtime'da yapılır
// 4. Facade Pattern - Dış dünya için basit bir arayüz sunar
// ═══════════════════════════════════════════════════════════════════════════════════════════════

using ECommerce.Core.Interfaces;
using ECommerce.Entities.Enums;
using System;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Payment;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Infrastructure.Services.Payment;
using ECommerce.Infrastructure.Services.Payment.Posnet;
using ECommerce.Infrastructure.Services.Payment.Posnet.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Genişletilmiş ödeme yönetimi interface'i
    /// POSNET 3D Secure ve özel işlemler için ek metodlar
    /// </summary>
    public interface IExtendedPaymentService : IPaymentService
    {
        /// <summary>POSNET 3D Secure ödeme başlatma</summary>
        Task<PaymentInitResult> Initiate3DSecureAsync(PaymentCreateDto dto, CancellationToken cancellationToken = default);
        
        /// <summary>POSNET direkt satış (2D)</summary>
        Task<PaymentInitResult> ProcessDirectSaleAsync(PaymentCreateDto dto, CancellationToken cancellationToken = default);
        
        /// <summary>Ödeme iptali (gün içi)</summary>
        Task<bool> CancelPaymentAsync(int paymentId, string? reason = null);
        
        /// <summary>Kısmi iade işlemi</summary>
        Task<bool> PartialRefundAsync(int paymentId, decimal amount);
        
        /// <summary>World Puan sorgulama (POSNET)</summary>
        Task<WorldPointsResult?> QueryWorldPointsAsync(string cardNumber, string expireDate, string cvv);
    }

    /// <summary>
    /// World Puan sorgu sonucu
    /// </summary>
    public class WorldPointsResult
    {
        public bool Success { get; set; }
        public int AvailablePoints { get; set; }
        public decimal PointsAsTL { get; set; }
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Merkezi Ödeme Yönetim Servisi
    /// Tüm ödeme sağlayıcılarını yönetir ve yönlendirir
    /// </summary>
    public class PaymentManager : IExtendedPaymentService
    {
        // ═══════════════════════════════════════════════════════════════════════════
        // DEPENDENCY INJECTION
        // ═══════════════════════════════════════════════════════════════════════════
        
        private readonly StripePaymentService? _stripe;
        private readonly IyzicoPaymentService _iyzico;
        private readonly PayPalPaymentService? _paypal;
        private readonly IPosnetPaymentService? _posnet; // Nullable - opsiyonel provider
        private readonly ECommerceDbContext _db;
        private readonly ILogService _logService;
        private readonly ILogger<PaymentManager>? _logger;
        private readonly string _defaultProvider;
        private readonly IConfiguration _configuration;

        // ═══════════════════════════════════════════════════════════════════════════
        // CONSTRUCTOR
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// PaymentManager constructor
        /// POSNET opsiyonel olarak inject edilir (null olabilir)
        /// </summary>
        public PaymentManager(
            IyzicoPaymentService iyzico,
            ECommerceDbContext db,
            ILogService logService,
            IConfiguration configuration,
            IPosnetPaymentService? posnet = null,
            ILogger<PaymentManager>? logger = null,
            StripePaymentService? stripe = null,
            PayPalPaymentService? paypal = null)
        {
            _iyzico = iyzico ?? throw new ArgumentNullException(nameof(iyzico));
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _logService = logService ?? throw new ArgumentNullException(nameof(logService));
            _posnet = posnet; // Opsiyonel
            _logger = logger;
            _stripe = stripe;
            _paypal = paypal;
            _configuration = configuration;
            
            // Varsayılan provider'ı configuration'dan al
            _defaultProvider = configuration["Payment:Provider"]?.ToLowerInvariant() ?? "stripe";
            
            _logger?.LogInformation(
                "PaymentManager initialized. DefaultProvider: {Provider}, POSNETEnabled: {PosnetEnabled}",
                _defaultProvider,
                _posnet != null);
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // PROVIDER RESOLUTION - Strategy Pattern
        // ═══════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// POSNET provider mı kontrol eder
        /// </summary>
        private bool IsPosnetKey(string? key)
        {
            if (string.IsNullOrWhiteSpace(key)) return false;
            var normalizedKey = key.Trim().ToLowerInvariant();
            return normalizedKey switch
            {
                "posnet" or "yapikredi" or "yapi_kredi" or "yapı kredi" or "yapikrediposnet" => true,
                _ => false
            };
        }

        /// <summary>
        /// Payment method string'ine göre uygun provider'ı seçer
        /// POSNET desteği eklendi
        /// </summary>
        private (IPaymentService service, string providerKey) ResolveProviderByMethod(string? method)
        {
            var key = (method ?? string.Empty).Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(key))
            {
                return ResolveDefaultProvider();
            }

            // POSNET / Yapı Kredi kontrolü
            if (IsPosnetKey(key))
            {
                if (_posnet != null)
                {
                    _logger?.LogDebug("Resolved provider: POSNET for key: {Key}", key);
                    return (_posnet, "posnet");
                }
                else
                {
                    _logger?.LogWarning("POSNET requested but service not registered. Falling back to default.");
                    return ResolveDefaultProvider();
                }
            }

            return key switch
            {
                "stripe" => ResolveUnavailableProviderFallback("stripe"),
                "paypal" => ResolveUnavailableProviderFallback("paypal"),
                "iyzico" or "iyzipay" => (_iyzico, "iyzico"),
                // Kredi kartı seçimi - varsayılan provider'a veya POSNET'e yönlendir
                "creditcard" or "credit_card" => ResolveCreditCardProvider(),
                _ => ResolveDefaultProvider()
            };
        }

        /// <summary>
        /// Kredi kartı ödemesi için provider seçimi
        /// Eğer POSNET aktifse ve varsayılan provider olarak ayarlandıysa POSNET'i kullan
        /// </summary>
        private (IPaymentService service, string providerKey) ResolveCreditCardProvider()
        {
            // Eğer varsayılan provider POSNET ise ve POSNET aktifse
            if (IsPosnetKey(_defaultProvider) && _posnet != null)
            {
                return (_posnet, "posnet");
            }
            
            // Aksi halde varsayılan provider'a dön
            return ResolveDefaultProvider();
        }

        /// <summary>
        /// Varsayılan ödeme provider'ını döndürür
        /// Configuration'dan okunur, POSNET desteği eklendi
        /// </summary>
        private (IPaymentService service, string providerKey) ResolveDefaultProvider()
        {
            var key = _defaultProvider;
            
            // POSNET varsayılan provider olarak ayarlandıysa
            if (IsPosnetKey(key) && _posnet != null)
            {
                return (_posnet, "posnet");
            }
            
            return key switch
            {
                "iyzico" or "iyzipay" => (_iyzico, "iyzico"),
                "paypal" => ResolveUnavailableProviderFallback("paypal"),
                "stripe" => ResolveUnavailableProviderFallback("stripe"),
                _ => (_iyzico, "iyzico")
            };
        }

        private (IPaymentService service, string providerKey) ResolveUnavailableProviderFallback(string requested)
        {
            _logger?.LogWarning("{Provider} provider disabled or not registered. Falling back to supported provider.", requested);

            if (_posnet != null)
            {
                return (_posnet, "posnet");
            }

            return (_iyzico, "iyzico");
        }

        /// <summary>
        /// Mevcut payment kaydından provider'ı belirler
        /// POSNET desteği eklendi
        /// </summary>
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

        /// <summary>
        /// Payment ID'den ödeme kaydını bulup provider'ı çözer
        /// </summary>
        private async Task<(Payments? payment, IPaymentService service, string providerKey)> ResolvePaymentAndProviderAsync(int paymentId)
        {
            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.Id == paymentId);
            if (payment == null)
            {
                var fallback = ResolveDefaultProvider();
                return (null, fallback.service, fallback.providerKey);
            }

            var resolved = ResolveProviderByMethod(payment.Provider);
            return (payment, resolved.service, resolved.providerKey);
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
            return ok ? PaymentStatus.Paid : PaymentStatus.Failed;
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
        /// POSNET için kart bilgileri kullanılır
        /// </summary>
        public async Task<PaymentInitResult> InitiateAsync(PaymentCreateDto dto)
        {
            if (dto == null) throw new ArgumentNullException(nameof(dto));

            var (service, providerKey) = ResolveProviderByMethod(dto.PaymentMethod);

            try
            {
                // POSNET provider ve kart bilgisi varsa özel akış
                if (providerKey == "posnet" && _posnet != null && dto.HasCardInfo)
                {
                    return await InitiatePosnetPaymentAsync(dto);
                }
                
                var result = await service.InitiateAsync(dto.OrderId, dto.Amount, dto.Currency ?? "TRY");
                return result;
            }
            catch (Exception ex)
            {
                await LogFailureAsync(dto.OrderId, dto.Amount, providerKey, "INITIATE_EXCEPTION", ex.Message, ex);
                throw;
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // POSNET ÖZEL METODLAR
        // ═══════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// POSNET ödeme akışını başlatır (3D veya 2D)
        /// </summary>
        private async Task<PaymentInitResult> InitiatePosnetPaymentAsync(PaymentCreateDto dto)
        {
            if (_posnet == null)
            {
                return new PaymentInitResult
                {
                    Success = false,
                    Error = "POSNET servisi aktif değil"
                };
            }

            _logger?.LogInformation(
                "Initiating POSNET payment. OrderId: {OrderId}, Amount: {Amount}, Use3D: {Use3D}",
                dto.OrderId, dto.Amount, dto.Use3DSecure);

            // 3D Secure akışı
            if (dto.Use3DSecure)
            {
                return await Initiate3DSecureAsync(dto);
            }

            // Direkt satış (2D)
            return await ProcessDirectSaleAsync(dto);
        }

        /// <summary>
        /// POSNET 3D Secure ödeme başlatma
        /// Müşteri banka sayfasına yönlendirilir
        /// </summary>
        public async Task<PaymentInitResult> Initiate3DSecureAsync(PaymentCreateDto dto, CancellationToken cancellationToken = default)
        {
            if (_posnet == null)
            {
                return new PaymentInitResult
                {
                    Success = false,
                    Error = "POSNET servisi aktif değil"
                };
            }

            if (!dto.HasCardInfo)
            {
                return new PaymentInitResult
                {
                    Success = false,
                    Error = "Kart bilgileri eksik"
                };
            }

            try
            {
                var result = await _posnet.Initiate3DSecureAsync(
                    dto.OrderId,
                    dto.CardNumber!,
                    dto.ExpireDate!,
                    dto.Cvv!,
                    dto.GetNormalizedInstallment(),
                    cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger?.LogInformation(
                        "POSNET 3D Secure initiated. OrderId: {OrderId}",
                        dto.OrderId);

                    return new PaymentInitResult
                    {
                        Success = true,
                        RedirectUrl = result.Data.RedirectUrl,
                        PaymentId = result.Data.OrderId ?? dto.OrderId.ToString(),
                        RequiresRedirect = result.Data.RequiresRedirect,
                        ThreeDSecureHtml = result.Data.GenerateAutoSubmitForm(
                            result.Data.RedirectUrl ?? "",
                            _configuration["Payment:PosnetMerchantId"] ?? string.Empty,
                            _configuration["Payment:PosnetId"] ?? string.Empty,
                            _configuration["Payment:PosnetCallbackUrl"] ?? dto.SuccessUrl ?? string.Empty),
                        Is3DSecure = true,
                        Provider = "posnet"
                    };
                }
                else
                {
                    await LogFailureAsync(dto.OrderId, dto.Amount, "posnet", "3DS_INIT", 
                        result.Error ?? "3D Secure başlatılamadı");

                    return new PaymentInitResult
                    {
                        Success = false,
                        Error = result.Error ?? "3D Secure başlatılamadı",
                        ErrorCode = result.ErrorCode.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET 3D Secure initiation failed. OrderId: {OrderId}", dto.OrderId);
                await LogFailureAsync(dto.OrderId, dto.Amount, "posnet", "3DS_EXCEPTION", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// POSNET direkt satış (3D Secure olmadan)
        /// PCI DSS uyumluluk gerektirir
        /// </summary>
        public async Task<PaymentInitResult> ProcessDirectSaleAsync(PaymentCreateDto dto, CancellationToken cancellationToken = default)
        {
            if (_posnet == null)
            {
                return new PaymentInitResult
                {
                    Success = false,
                    Error = "POSNET servisi aktif değil"
                };
            }

            if (!dto.HasCardInfo)
            {
                return new PaymentInitResult
                {
                    Success = false,
                    Error = "Kart bilgileri eksik"
                };
            }

            try
            {
                var result = await _posnet.ProcessDirectSaleAsync(
                    dto.OrderId,
                    dto.CardNumber!,
                    dto.ExpireDate!,
                    dto.Cvv!,
                    dto.GetNormalizedInstallment(),
                    cancellationToken);

                if (result.IsSuccess && result.Data != null)
                {
                    _logger?.LogInformation(
                        "POSNET direct sale completed. OrderId: {OrderId}, HostLogKey: {HostLogKey}",
                        dto.OrderId, result.Data.HostLogKey);

                    return new PaymentInitResult
                    {
                        Success = true,
                        PaymentId = result.Data.HostLogKey,
                        TransactionId = result.Data.TransactionId,
                        HostLogKey = result.Data.HostLogKey,
                        AuthCode = result.Data.AuthCode,
                        Provider = "posnet"
                    };
                }
                else
                {
                    await LogFailureAsync(dto.OrderId, dto.Amount, "posnet", "DIRECT_SALE", 
                        result.Error ?? "Direkt satış başarısız");

                    return new PaymentInitResult
                    {
                        Success = false,
                        Error = result.Error ?? "Direkt satış başarısız",
                        ErrorCode = result.ErrorCode.ToString()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "POSNET direct sale failed. OrderId: {OrderId}", dto.OrderId);
                await LogFailureAsync(dto.OrderId, dto.Amount, "posnet", "DIRECT_SALE_EXCEPTION", ex.Message, ex);
                throw;
            }
        }

        /// <summary>
        /// Ödeme iptali (gün içi)
        /// POSNET için reverse, diğerleri için genel iptal
        /// </summary>
        public async Task<bool> CancelPaymentAsync(int paymentId, string? reason = null)
        {
            var (payment, service, providerKey) = await ResolvePaymentAndProviderAsync(paymentId);
            
            if (payment == null)
            {
                _logger?.LogWarning("Cancel failed: Payment not found. Id: {PaymentId}", paymentId);
                return false;
            }

            try
            {
                // POSNET için reverse işlemi
                if (providerKey == "posnet" && _posnet != null)
                {
                    var hostLogKey = payment.HostLogKey;
                    // HostLogKey boşsa ProviderPaymentId'den al (3DS callback'de HostLogKey kayıp olabilir)
                    if (string.IsNullOrEmpty(hostLogKey))
                    {
                        hostLogKey = payment.ProviderPaymentId;
                        _logger?.LogWarning("Cancel: HostLogKey boş, ProviderPaymentId fallback. Id: {PaymentId}, Key: {Key}", paymentId, hostLogKey);
                    }
                    if (string.IsNullOrEmpty(hostLogKey))
                    {
                        _logger?.LogWarning("Cancel failed: HostLogKey ve ProviderPaymentId boş. Id: {PaymentId}", paymentId);
                        return false;
                    }

                    var result = await _posnet.ProcessReverseAsync(payment.OrderId, hostLogKey);
                    
                    if (result.IsSuccess)
                    {
                        payment.Status = "Cancelled";
                        payment.UpdatedAt = DateTime.UtcNow;
                        
                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                        if (order != null)
                        {
                            order.Status = OrderStatus.Cancelled;
                        }
                        
                        await _db.SaveChangesAsync();

                        _logService.Audit(
                            action: "PAYMENT_CANCELLED",
                            entityName: "Payments",
                            entityId: paymentId,
                            oldValues: new { Status = "Paid" },
                            newValues: new { Status = "Cancelled", Reason = reason },
                            performedBy: null);

                        _logger?.LogInformation("POSNET payment cancelled. Id: {PaymentId}, Reason: {Reason}", paymentId, reason);
                        return true;
                    }
                    else
                    {
                        _logger?.LogWarning("POSNET cancel failed. Id: {PaymentId}, Error: {Error}", paymentId, result.Error);
                        return false;
                    }
                }

                // Diğer provider'lar için basit durum güncelleme
                payment.Status = "Cancelled";
                payment.UpdatedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync();
                
                _logger?.LogInformation("Payment cancelled (local). Id: {PaymentId}", paymentId);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Cancel payment failed. Id: {PaymentId}", paymentId);
                return false;
            }
        }

        /// <summary>
        /// Kısmi iade işlemi
        /// POSNET için return, diğerleri için provider'a göre işlem
        /// </summary>
        public async Task<bool> PartialRefundAsync(int paymentId, decimal amount)
        {
            var (payment, service, providerKey) = await ResolvePaymentAndProviderAsync(paymentId);
            
            if (payment == null)
            {
                _logger?.LogWarning("Partial refund failed: Payment not found. Id: {PaymentId}", paymentId);
                return false;
            }

            if (amount <= 0 || amount > payment.Amount)
            {
                _logger?.LogWarning("Partial refund failed: Invalid amount. Id: {PaymentId}, Amount: {Amount}", paymentId, amount);
                return false;
            }

            try
            {
                // POSNET için return işlemi
                if (providerKey == "posnet" && _posnet != null)
                {
                    var hostLogKey = payment.HostLogKey;
                    // HostLogKey boşsa ProviderPaymentId'den al (3DS callback'de HostLogKey kayıp olabilir)
                    if (string.IsNullOrEmpty(hostLogKey))
                    {
                        hostLogKey = payment.ProviderPaymentId;
                        _logger?.LogWarning("Refund: HostLogKey boş, ProviderPaymentId fallback. Id: {PaymentId}, Key: {Key}", paymentId, hostLogKey);
                    }
                    if (string.IsNullOrEmpty(hostLogKey))
                    {
                        _logger?.LogWarning("Partial refund failed: HostLogKey ve ProviderPaymentId boş. Id: {PaymentId}", paymentId);
                        return false;
                    }

                    var result = await _posnet.ProcessRefundAsync(payment.OrderId, hostLogKey, amount);
                    
                    if (result.IsSuccess)
                    {
                        // Kısmi iade kaydı
                        payment.RefundedAmount = (payment.RefundedAmount ?? 0) + amount;
                        payment.UpdatedAt = DateTime.UtcNow;
                        
                        // Tam iade olduysa status güncelle
                        if (payment.RefundedAmount >= payment.Amount)
                        {
                            payment.Status = "Refunded";
                            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                            if (order != null)
                            {
                                order.Status = OrderStatus.Refunded;
                            }
                        }
                        else
                        {
                            payment.Status = "PartiallyRefunded";
                        }
                        
                        await _db.SaveChangesAsync();

                        _logService.Audit(
                            action: "PAYMENT_REFUNDED",
                            entityName: "Payments",
                            entityId: paymentId,
                            oldValues: new { RefundedAmount = (payment.RefundedAmount ?? 0) - amount },
                            newValues: new { RefundedAmount = payment.RefundedAmount, Amount = amount },
                            performedBy: null);

                        _logger?.LogInformation("POSNET partial refund completed. Id: {PaymentId}, Amount: {Amount}", paymentId, amount);
                        return true;
                    }
                    else
                    {
                        _logger?.LogWarning("POSNET partial refund failed. Id: {PaymentId}, Error: {Error}", paymentId, result.Error);
                        return false;
                    }
                }

                // Stripe için
                if (providerKey == "stripe")
                {
                    var ok = await _stripe.StripeRefundAsync(payment.ProviderPaymentId, amount);
                    if (ok)
                    {
                        payment.RefundedAmount = (payment.RefundedAmount ?? 0) + amount;
                        payment.Status = payment.RefundedAmount >= payment.Amount ? "Refunded" : "PartiallyRefunded";
                        payment.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        return true;
                    }
                }

                // Iyzico için
                if (providerKey == "iyzico")
                {
                    var ok = await _iyzico.IyzicoRefundAsync(payment.ProviderPaymentId, amount);
                    if (ok)
                    {
                        payment.RefundedAmount = (payment.RefundedAmount ?? 0) + amount;
                        payment.Status = payment.RefundedAmount >= payment.Amount ? "Refunded" : "PartiallyRefunded";
                        payment.UpdatedAt = DateTime.UtcNow;
                        await _db.SaveChangesAsync();
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Partial refund failed. Id: {PaymentId}, Amount: {Amount}", paymentId, amount);
                return false;
            }
        }

        /// <summary>
        /// World Puan sorgulama (Sadece POSNET)
        /// </summary>
        public async Task<WorldPointsResult?> QueryWorldPointsAsync(string cardNumber, string expireDate, string cvv)
        {
            if (_posnet == null)
            {
                return new WorldPointsResult
                {
                    Success = false,
                    ErrorMessage = "POSNET servisi aktif değil"
                };
            }

            try
            {
                var result = await _posnet.QueryPointsAsync(cardNumber, expireDate, cvv);
                
                if (result.IsSuccess && result.Data != null)
                {
                    return new WorldPointsResult
                    {
                        Success = true,
                        AvailablePoints = result.Data.PointInfo?.TotalPoint ?? 0,
                        PointsAsTL = result.Data.PointInfo?.PointAsTL ?? 0
                    };
                }
                
                return new WorldPointsResult
                {
                    Success = false,
                    ErrorMessage = result.Error ?? "Puan sorgulanamadı"
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "World points query failed");
                return new WorldPointsResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        // ═══════════════════════════════════════════════════════════════════════════
        // REFUND İŞLEMİ - POSNET DESTEĞİ EKLENDİ
        // ═══════════════════════════════════════════════════════════════════════════

        public async Task<bool> RefundAsync(Core.DTOs.Payment.PaymentRefundRequestDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.PaymentId)) return false;

            var payment = await _db.Payments.FirstOrDefaultAsync(p => p.ProviderPaymentId == dto.PaymentId || p.Id.ToString() == dto.PaymentId);
            if (payment == null) return false;

            // İade tutarı belirtilmemişse tam iade yap
            var refundAmount = dto.Amount ?? payment.Amount;

            var provider = payment.Provider?.ToLowerInvariant() ?? string.Empty;
            
            try
            {
                // POSNET için return işlemi
                if (IsPosnetKey(provider) && _posnet != null)
                {
                    var hostLogKey = payment.HostLogKey;
                    if (string.IsNullOrEmpty(hostLogKey))
                    {
                        _logger?.LogWarning("POSNET refund failed: HostLogKey is empty. PaymentId: {PaymentId}", dto.PaymentId);
                        // HostLogKey yoksa ProviderPaymentId dene
                        hostLogKey = payment.ProviderPaymentId;
                    }

                    var result = await _posnet.ProcessRefundAsync(payment.OrderId, hostLogKey, refundAmount);
                    
                    if (result.IsSuccess)
                    {
                        payment.Status = "Refunded";
                        payment.RefundedAmount = refundAmount;
                        payment.UpdatedAt = DateTime.UtcNow;
                        
                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                        if (order != null) order.Status = OrderStatus.Refunded;
                        
                        await _db.SaveChangesAsync();

                        _logService.Audit(
                            action: "PAYMENT_REFUNDED",
                            entityName: "Payments",
                            entityId: payment.Id,
                            oldValues: new { Status = "Paid" },
                            newValues: new { Status = "Refunded", Amount = refundAmount },
                            performedBy: null);

                        _logger?.LogInformation("POSNET refund completed. PaymentId: {PaymentId}, Amount: {Amount}", dto.PaymentId, refundAmount);
                        return true;
                    }
                    else
                    {
                        _logger?.LogWarning("POSNET refund failed. PaymentId: {PaymentId}, Error: {Error}", dto.PaymentId, result.Error);
                        return false;
                    }
                }
                
                // Stripe için iade
                if (provider == "stripe")
                {
                    var ok = await _stripe.StripeRefundAsync(payment.ProviderPaymentId, refundAmount);
                    if (ok)
                    {
                        payment.Status = "Refunded";
                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                        if (order != null) order.Status = Entities.Enums.OrderStatus.Refunded;
                        await _db.SaveChangesAsync();
                        return true;
                    }
                }
                // Iyzico için iade
                else if (provider == "iyzico")
                {
                    var ok = await _iyzico.IyzicoRefundAsync(payment.ProviderPaymentId, refundAmount);
                    if (ok)
                    {
                        payment.Status = "Refunded";
                        var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                        if (order != null) order.Status = Entities.Enums.OrderStatus.Refunded;
                        await _db.SaveChangesAsync();
                        return true;
                    }
                }

                // Fallback: mark refunded locally
                payment.Status = "Refunded";
                var ord = await _db.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
                if (ord != null) ord.Status = Entities.Enums.OrderStatus.Refunded;
                await _db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await LogFailureAsync(payment.OrderId, payment.Amount, provider, "REFUND_EXCEPTION", ex.Message, ex, payment.ProviderPaymentId);
                return false;
            }
        }
    }
}
