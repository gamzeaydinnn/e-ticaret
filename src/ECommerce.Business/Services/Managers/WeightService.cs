using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Ağırlık raporu işleme servisi.
    /// 
    /// Mikro API (tartı cihazı) entegrasyonu:
    /// - Webhook ile gelen tartı verilerini işler
    /// - Kurye manuel tartı farkı girişini destekler
    /// - Final tutar hesaplaması yapar
    /// 
    /// Güvenlik:
    /// - Idempotency kontrolü (ExternalReportId)
    /// - Sipariş ownership doğrulaması
    /// </summary>
    public class WeightService : IWeightService
    {
        private readonly IWeightReportRepository _weightReportRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentService _paymentService;
        private readonly IPaymentCaptureService _paymentCaptureService;
        private readonly ILogger<WeightService> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _autoApproveThresholdGrams;

        public WeightService(
            IWeightReportRepository weightReportRepository,
            IOrderRepository orderRepository,
            IPaymentService paymentService,
            IPaymentCaptureService paymentCaptureService,
            ILogger<WeightService> logger,
            IConfiguration configuration)
        {
            _weightReportRepository = weightReportRepository;
            _orderRepository = orderRepository;
            _paymentService = paymentService;
            _paymentCaptureService = paymentCaptureService;
            _logger = logger;
            _configuration = configuration;

            // Otomatik onay eşik değeri: 0 (her fazlalık için manuel onay gerekli)
            _autoApproveThresholdGrams = 0;
        }

        public async Task<WeightReport> ProcessReportAsync(MicroWeightReportDto dto)
        {
            try
            {
                // Idempotency kontrolü
                var existingReport = await _weightReportRepository.GetByExternalReportIdAsync(dto.ReportId);
                if (existingReport != null)
                {
                    _logger.LogInformation($"Rapor zaten mevcut: {dto.ReportId}");
                    return existingReport;
                }

                // Siparişi getir
                var order = await _orderRepository.GetByIdAsync(dto.OrderId);
                if (order == null)
                {
                    _logger.LogWarning($"Sipariş bulunamadı: {dto.OrderId}");
                    throw new ArgumentException($"Sipariş bulunamadı: {dto.OrderId}");
                }

                // Beklenen ağırlığı hesapla
                int expectedWeightGrams;
                if (dto.OrderItemId.HasValue)
                {
                    var orderItem = order.OrderItems.FirstOrDefault(oi => oi.Id == dto.OrderItemId.Value);
                    expectedWeightGrams = orderItem?.ExpectedWeightGrams ?? 0;
                }
                else
                {
                    // Tüm sipariş için toplam ağırlık
                    expectedWeightGrams = order.OrderItems.Sum(oi => oi.ExpectedWeightGrams);
                }

                // Fazla gramaj hesapla
                var overageGrams = Math.Max(0, dto.ReportedWeightGrams - expectedWeightGrams);

                // Fazla tutar hesapla
                decimal overageAmount = 0;
                if (overageGrams > 0)
                {
                    // Gram başına fiyat hesapla
                    decimal pricePerGram = expectedWeightGrams > 0 
                        ? order.TotalPrice / expectedWeightGrams 
                        : 0;
                    
                    overageAmount = Math.Round(overageGrams * pricePerGram, 2);
                }

                // WeightReport oluştur
                var weightReport = new WeightReport
                {
                    ExternalReportId = dto.ReportId,
                    OrderId = dto.OrderId,
                    OrderItemId = dto.OrderItemId,
                    ExpectedWeightGrams = expectedWeightGrams,
                    ReportedWeightGrams = dto.ReportedWeightGrams,
                    OverageGrams = overageGrams,
                    OverageAmount = overageAmount,
                    Currency = order.Currency,
                    Status = overageGrams > 0 ? WeightReportStatus.Pending : WeightReportStatus.AutoApproved,
                    Source = dto.Source,
                    ReceivedAt = dto.Timestamp,
                    Metadata = dto.Metadata,
                    CreatedAt = DateTime.UtcNow
                };

                // 1 gram bile fazlalık varsa manuel onay gerekli
                if (overageGrams > 0)
                {
                    _logger.LogInformation($"Ağırlık fazlalığı tespit edildi: {overageGrams}g = {overageAmount} {order.Currency}, manuel onay gerekli.");
                    weightReport.Status = WeightReportStatus.Pending;
                }
                else
                {
                    _logger.LogInformation($"Ağırlık farkı yok, otomatik onaylandı.");
                    weightReport.Status = WeightReportStatus.AutoApproved;
                    weightReport.ProcessedAt = DateTimeOffset.UtcNow;
                }

                await _weightReportRepository.AddAsync(weightReport);

                _logger.LogInformation($"Ağırlık raporu oluşturuldu: {weightReport.Id}, Fazlalık: {overageGrams}g, Tutar: {overageAmount} {order.Currency}");

                return weightReport;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ağırlık raporu işlenirken hata: {dto.ReportId}");
                throw;
            }
        }

        public async Task<bool> ApproveReportAsync(int reportId, int approvedByUserId, string? note = null)
        {
            try
            {
                var report = await _weightReportRepository.GetByIdAsync(reportId);
                if (report == null)
                {
                    _logger.LogWarning($"Rapor bulunamadı: {reportId}");
                    return false;
                }

                if (report.Status != WeightReportStatus.Pending)
                {
                    _logger.LogWarning($"Rapor onaylanamaz durumda: {report.Status}");
                    return false;
                }

                report.Status = WeightReportStatus.Approved;
                report.ApprovedByUserId = approvedByUserId;
                report.ApprovedAt = DateTimeOffset.UtcNow;
                report.ProcessedAt = DateTimeOffset.UtcNow;
                report.AdminNote = note;
                report.UpdatedAt = DateTime.UtcNow;

                await _weightReportRepository.UpdateAsync(report);

                // Ödeme tahsilatını başlat
                if (report.OverageAmount > 0)
                {
                    await ChargeOverageAsync(reportId);
                }

                _logger.LogInformation($"Rapor onaylandı: {reportId}, Onaylayan: {approvedByUserId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor onaylanırken hata: {reportId}");
                return false;
            }
        }

        public async Task<bool> RejectReportAsync(int reportId, int rejectedByUserId, string? reason = null)
        {
            try
            {
                var report = await _weightReportRepository.GetByIdAsync(reportId);
                if (report == null)
                {
                    _logger.LogWarning($"Rapor bulunamadı: {reportId}");
                    return false;
                }

                report.Status = WeightReportStatus.Rejected;
                report.ApprovedByUserId = rejectedByUserId;
                report.ApprovedAt = DateTimeOffset.UtcNow;
                report.ProcessedAt = DateTimeOffset.UtcNow;
                report.AdminNote = reason;
                report.UpdatedAt = DateTime.UtcNow;

                await _weightReportRepository.UpdateAsync(report);

                _logger.LogInformation($"Rapor reddedildi: {reportId}, Reddeden: {rejectedByUserId}, Sebep: {reason}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Rapor reddedilirken hata: {reportId}");
                return false;
            }
        }

        public async Task<bool> ChargeOverageAsync(int reportId)
        {
            try
            {
                var report = await _weightReportRepository.GetByIdAsync(reportId);
                if (report == null || report.Order == null)
                {
                    _logger.LogWarning($"Rapor veya sipariş bulunamadı: {reportId}");
                    return false;
                }

                if (report.OverageAmount <= 0)
                {
                    _logger.LogInformation($"Tahsil edilecek tutar yok: {reportId}");
                    report.Status = WeightReportStatus.Charged;
                    report.ProcessedAt = DateTimeOffset.UtcNow;
                    report.UpdatedAt = DateTime.UtcNow;
                    await _weightReportRepository.UpdateAsync(report);
                    return true;
                }

                // Sipariş için tartı dahil final tutarı hesapla
                var finalAmount = await CalculateFinalAmountForOrderAsync(report.OrderId);
                _logger.LogInformation(
                    "Ağırlık farkı capture başlıyor: ReportId={ReportId}, FazlaTutar={OverageAmount}, FinalTutar={FinalAmount}",
                    reportId, report.OverageAmount, finalAmount);

                // PaymentCaptureService üzerinden gerçek POSNET capture
                var captureResult = await _paymentCaptureService.CapturePaymentAsync(report.OrderId, finalAmount);

                if (captureResult.Success)
                {
                    report.Status = WeightReportStatus.Charged;
                    report.PaymentAttemptId = captureResult.CaptureReference;
                    report.ProcessedAt = DateTimeOffset.UtcNow;
                    report.UpdatedAt = DateTime.UtcNow;
                    await _weightReportRepository.UpdateAsync(report);

                    _logger.LogInformation(
                        "Fazla tutar başarıyla tahsil edildi: ReportId={ReportId}, Tutar={Amount} {Currency}, CaptureRef={Ref}",
                        reportId, report.OverageAmount, report.Currency, captureResult.CaptureReference);
                    return true;
                }
                else
                {
                    report.Status = WeightReportStatus.Failed;
                    report.AdminNote = $"Ödeme hatası: {captureResult.Message}";
                    report.ProcessedAt = DateTimeOffset.UtcNow;
                    report.UpdatedAt = DateTime.UtcNow;
                    await _weightReportRepository.UpdateAsync(report);

                    _logger.LogWarning(
                        "Fazla tutar tahsilatı başarısız: ReportId={ReportId}, Hata={Error}, ErrorCode={Code}",
                        reportId, captureResult.Message, captureResult.ErrorCode);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fazla tutar tahsil edilirken hata: {reportId}");
                return false;
            }
        }

        public async Task<WeightReport?> GetReportByIdAsync(int reportId)
        {
            return await _weightReportRepository.GetByIdAsync(reportId);
        }

        public async Task<WeightReport?> GetReportByExternalIdAsync(string externalReportId)
        {
            return await _weightReportRepository.GetByExternalReportIdAsync(externalReportId);
        }

        #region Yeni Webhook ve Kurye Entegrasyon Metotları

        /// <summary>
        /// Webhook'tan gelen tartı verisini işler.
        /// 
        /// Akış:
        /// 1. Idempotency kontrolü (ExternalReportId)
        /// 2. Sipariş doğrulama
        /// 3. WeightReport oluşturma
        /// 4. OrderItem.ActualWeight güncelleme
        /// 5. Fark hesaplama
        /// </summary>
        public async Task<MicroWeightWebhookResponseDto> ProcessWebhookAsync(MicroWeightWebhookRequestDto dto)
        {
            try
            {
                _logger.LogInformation("Webhook tartı verisi alındı: ReportId={ReportId}, OrderId={OrderId}, Weight={Weight}g",
                    dto.ReportId, dto.OrderId, dto.ReportedWeightGrams);

                // 1. Idempotency kontrolü
                var existingReport = await _weightReportRepository.GetByExternalReportIdAsync(dto.ReportId);
                if (existingReport != null)
                {
                    _logger.LogInformation("Rapor zaten mevcut, idempotent yanıt dönülüyor: {ReportId}", dto.ReportId);
                    return new MicroWeightWebhookResponseDto
                    {
                        Success = true,
                        Message = "Rapor zaten işlenmiş",
                        ReportId = existingReport.Id,
                        Status = existingReport.Status.ToString(),
                        OverageGrams = existingReport.OverageGrams,
                        OverageAmount = existingReport.OverageAmount
                    };
                }

                // 2. Siparişi doğrula
                var order = await _orderRepository.GetByIdAsync(dto.OrderId);
                if (order == null)
                {
                    _logger.LogWarning("Sipariş bulunamadı: {OrderId}", dto.OrderId);
                    return new MicroWeightWebhookResponseDto
                    {
                        Success = false,
                        Message = "Sipariş bulunamadı",
                        ErrorCode = "ORDER_NOT_FOUND"
                    };
                }

                // 3. MicroWeightReportDto'ya dönüştür ve işle
                var microDto = new MicroWeightReportDto
                {
                    ReportId = dto.ReportId,
                    OrderId = dto.OrderId,
                    OrderItemId = dto.OrderItemId,
                    ReportedWeightGrams = dto.ReportedWeightGrams,
                    Source = dto.Source,
                    Timestamp = dto.Timestamp,
                    Metadata = dto.Metadata
                };

                var report = await ProcessReportAsync(microDto);

                // 4. OrderItem güncelle (eğer belirli bir item için ise)
                await UpdateOrderItemActualWeightAsync(order, dto.OrderItemId, dto.ReportedWeightGrams);

                // 5. Kurye notu varsa ekle
                if (!string.IsNullOrWhiteSpace(dto.CourierNote))
                {
                    report.CourierNote = dto.CourierNote;
                    await _weightReportRepository.UpdateAsync(report);
                }

                _logger.LogInformation("Webhook tartı verisi başarıyla işlendi: ReportId={ReportId}, Overage={OverageGrams}g",
                    dto.ReportId, report.OverageGrams);

                return new MicroWeightWebhookResponseDto
                {
                    Success = true,
                    Message = report.OverageGrams > 0 
                        ? $"Tartı farkı tespit edildi: {report.OverageGrams}g fazla"
                        : "Tartı kaydedildi",
                    ReportId = report.Id,
                    Status = report.Status.ToString(),
                    OverageGrams = report.OverageGrams,
                    OverageAmount = report.OverageAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook tartı verisi işlenirken hata: {ReportId}", dto.ReportId);
                return new MicroWeightWebhookResponseDto
                {
                    Success = false,
                    Message = "Tartı verisi işlenirken hata oluştu",
                    ErrorCode = "PROCESSING_ERROR"
                };
            }
        }

        /// <summary>
        /// Kurye tarafından manuel girilen tartı farkını işler.
        /// 
        /// Kullanım Senaryoları:
        /// - Mikro API yokken kurye teslimat sırasında tartı farkı bildirir
        /// - Paket hasarlı/eksik geldiğinde fark kaydı
        /// </summary>
        public async Task<CourierWeightAdjustmentResponseDto> ProcessCourierAdjustmentAsync(
            CourierWeightAdjustmentDto dto, int courierId)
        {
            try
            {
                _logger.LogInformation("Kurye #{CourierId} tartı farkı bildirdi: OrderId={OrderId}, Fark={Diff}g",
                    courierId, dto.OrderId, dto.WeightDifferenceGrams);

                // 1. Siparişi doğrula
                var order = await _orderRepository.GetByIdAsync(dto.OrderId);
                if (order == null)
                {
                    return new CourierWeightAdjustmentResponseDto
                    {
                        Success = false,
                        Message = "Sipariş bulunamadı"
                    };
                }

                // 2. Kurye ownership kontrolü
                if (order.CourierId != courierId)
                {
                    _logger.LogWarning("Yetkisiz tartı farkı girişi denemesi: Kurye #{CourierId} -> Sipariş #{OrderId}",
                        courierId, dto.OrderId);
                    return new CourierWeightAdjustmentResponseDto
                    {
                        Success = false,
                        Message = "Bu siparişe erişim yetkiniz yok"
                    };
                }

                // 3. Beklenen ağırlığı hesapla
                int expectedWeightGrams;
                if (dto.OrderItemId.HasValue)
                {
                    var orderItem = order.OrderItems?.FirstOrDefault(oi => oi.Id == dto.OrderItemId.Value);
                    if (orderItem == null)
                    {
                        return new CourierWeightAdjustmentResponseDto
                        {
                            Success = false,
                            Message = "Sipariş kalemi bulunamadı"
                        };
                    }
                    expectedWeightGrams = orderItem.ExpectedWeightGrams;
                }
                else
                {
                    expectedWeightGrams = order.OrderItems?.Sum(oi => oi.ExpectedWeightGrams) ?? 0;
                }

                // 4. Gerçek ağırlığı hesapla
                var reportedWeightGrams = expectedWeightGrams + dto.WeightDifferenceGrams;
                if (reportedWeightGrams < 0)
                {
                    return new CourierWeightAdjustmentResponseDto
                    {
                        Success = false,
                        Message = "Geçersiz ağırlık farkı: Toplam ağırlık negatif olamaz"
                    };
                }

                // 5. Benzersiz rapor ID oluştur
                var externalReportId = $"COURIER_{courierId}_{dto.OrderId}_{DateTime.UtcNow:yyyyMMddHHmmss}";

                // 6. MicroWeightReportDto oluştur ve işle
                var microDto = new MicroWeightReportDto
                {
                    ReportId = externalReportId,
                    OrderId = dto.OrderId,
                    OrderItemId = dto.OrderItemId,
                    ReportedWeightGrams = reportedWeightGrams,
                    Source = $"Courier#{courierId}",
                    Timestamp = DateTimeOffset.UtcNow,
                    Metadata = dto.Note != null ? $"{{\"courierNote\":\"{dto.Note}\"}}" : null
                };

                var report = await ProcessReportAsync(microDto);

                // 7. Kurye notunu kaydet
                if (!string.IsNullOrWhiteSpace(dto.Note))
                {
                    report.CourierNote = dto.Note;
                    await _weightReportRepository.UpdateAsync(report);
                }

                // 8. OrderItem güncelle
                await UpdateOrderItemActualWeightAsync(order, dto.OrderItemId, reportedWeightGrams);

                // 9. Fark tutarını hesapla
                var differenceAmount = report.OverageGrams > 0 ? report.OverageAmount : 
                    (dto.WeightDifferenceGrams < 0 ? CalculateRefundAmount(order, Math.Abs(dto.WeightDifferenceGrams)) : 0);

                // 10. Yeni toplam tutarı hesapla
                var newTotalAmount = await CalculateFinalAmountForOrderAsync(dto.OrderId);

                _logger.LogInformation("Kurye tartı farkı kaydedildi: ReportId={ReportId}, Fark={Diff}g, Tutar={Amount}",
                    report.Id, dto.WeightDifferenceGrams, differenceAmount);

                return new CourierWeightAdjustmentResponseDto
                {
                    Success = true,
                    Message = dto.WeightDifferenceGrams > 0 
                        ? $"Fazla tartı kaydedildi: {dto.WeightDifferenceGrams}g"
                        : dto.WeightDifferenceGrams < 0 
                            ? $"Eksik tartı kaydedildi: {Math.Abs(dto.WeightDifferenceGrams)}g"
                            : "Tartı farkı yok",
                    ReportId = report.Id,
                    DifferenceAmount = differenceAmount,
                    NewTotalAmount = newTotalAmount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kurye tartı farkı işlenirken hata: OrderId={OrderId}", dto.OrderId);
                return new CourierWeightAdjustmentResponseDto
                {
                    Success = false,
                    Message = "Tartı farkı işlenirken hata oluştu"
                };
            }
        }

        /// <summary>
        /// Sipariş için tartı bazlı final tutarı hesaplar.
        /// 
        /// Hesaplama:
        /// - Orijinal sipariş tutarı
        /// + Onaylanan fazla ağırlık tutarları
        /// - Reddedilen/eksik ağırlık iade tutarları
        /// </summary>
        public async Task<decimal> CalculateFinalAmountForOrderAsync(int orderId)
        {
            try
            {
                var order = await _orderRepository.GetByIdAsync(orderId);
                if (order == null)
                {
                    _logger.LogWarning("Final tutar hesaplama: Sipariş bulunamadı: {OrderId}", orderId);
                    return 0;
                }

                // Orijinal sipariş tutarı
                var baseAmount = order.TotalPrice;

                // Onaylanan tartı farkı tutarlarını topla
                var approvedOverageAmount = await GetTotalWeightDifferenceAmountAsync(orderId);

                var finalAmount = baseAmount + approvedOverageAmount;

                _logger.LogDebug("Final tutar hesaplandı: OrderId={OrderId}, Base={Base}, Overage={Overage}, Final={Final}",
                    orderId, baseAmount, approvedOverageAmount, finalAmount);

                return Math.Max(0, finalAmount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final tutar hesaplanırken hata: OrderId={OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Sipariş için toplam onaylanan tartı farkı tutarını getirir.
        /// </summary>
        public async Task<decimal> GetTotalWeightDifferenceAmountAsync(int orderId)
        {
            try
            {
                var reports = await _weightReportRepository.GetByOrderIdAsync(orderId);
                
                // Sadece onaylanan veya otomatik onaylanan raporları dahil et
                var approvedAmount = reports
                    .Where(r => r.Status == WeightReportStatus.Approved || 
                               r.Status == WeightReportStatus.AutoApproved ||
                               r.Status == WeightReportStatus.Charged)
                    .Sum(r => r.OverageAmount);

                return approvedAmount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tartı farkı tutarı hesaplanırken hata: OrderId={OrderId}", orderId);
                return 0;
            }
        }

        /// <summary>
        /// Sipariş için bekleyen (admin onayı bekleyen) tartı raporlarını getirir.
        /// </summary>
        public async Task<IEnumerable<WeightReport>> GetPendingReportsForOrderAsync(int orderId)
        {
            try
            {
                var reports = await _weightReportRepository.GetByOrderIdAsync(orderId);
                return reports.Where(r => r.Status == WeightReportStatus.Pending);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bekleyen raporlar alınırken hata: OrderId={OrderId}", orderId);
                return Enumerable.Empty<WeightReport>();
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// OrderItem'ın ActualWeight alanını günceller.
        /// </summary>
        private async Task UpdateOrderItemActualWeightAsync(Order order, int? orderItemId, int reportedWeightGrams)
        {
            try
            {
                if (order.OrderItems == null || !order.OrderItems.Any())
                    return;

                if (orderItemId.HasValue)
                {
                    // Belirli bir item için güncelle
                    var item = order.OrderItems.FirstOrDefault(oi => oi.Id == orderItemId.Value);
                    if (item != null)
                    {
                        item.ActualWeight = reportedWeightGrams;
                        item.WeightDifference = reportedWeightGrams - (decimal)item.ExpectedWeightGrams;
                        item.IsWeighed = true;
                        item.WeighedAt = DateTime.UtcNow;
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                }
                else
                {
                    // Tüm sipariş için orantılı dağıt
                    var totalExpected = order.OrderItems.Sum(oi => oi.ExpectedWeightGrams);
                    if (totalExpected > 0)
                    {
                        foreach (var item in order.OrderItems)
                        {
                            var ratio = (decimal)item.ExpectedWeightGrams / totalExpected;
                            item.ActualWeight = reportedWeightGrams * ratio;
                            item.WeightDifference = item.ActualWeight - (decimal)item.ExpectedWeightGrams;
                            item.IsWeighed = true;
                            item.WeighedAt = DateTime.UtcNow;
                            item.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _orderRepository.UpdateAsync(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "OrderItem ağırlık güncelleme hatası: OrderId={OrderId}", order.Id);
            }
        }

        /// <summary>
        /// Eksik ağırlık için iade tutarını hesaplar.
        /// </summary>
        private decimal CalculateRefundAmount(Order order, int shortageGrams)
        {
            if (shortageGrams <= 0) return 0;

            var expectedWeight = order.OrderItems?.Sum(oi => oi.ExpectedWeightGrams) ?? 0;
            if (expectedWeight <= 0) return 0;

            var pricePerGram = order.TotalPrice / expectedWeight;
            return Math.Round(shortageGrams * pricePerGram, 2);
        }

        #endregion
    }
}
