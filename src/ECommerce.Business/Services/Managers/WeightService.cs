using System;
using System.Linq;
using System.Threading.Tasks;
using ECommerce.Core.DTOs.Weight;
using ECommerce.Core.Interfaces;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Ağırlık raporu işleme servisi
    /// </summary>
    public class WeightService : IWeightService
    {
        private readonly IWeightReportRepository _weightReportRepository;
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<WeightService> _logger;
        private readonly IConfiguration _configuration;
        private readonly int _autoApproveThresholdGrams;

        public WeightService(
            IWeightReportRepository weightReportRepository,
            IOrderRepository orderRepository,
            IPaymentService paymentService,
            ILogger<WeightService> logger,
            IConfiguration configuration)
        {
            _weightReportRepository = weightReportRepository;
            _orderRepository = orderRepository;
            _paymentService = paymentService;
            _logger = logger;
            _configuration = configuration;
            
            // Otomatik onay eşik değeri (varsayılan 50 gram)
            _autoApproveThresholdGrams = _configuration.GetValue<int>("Micro:AutoApproveThresholdGrams", 50);
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
                    Status = WeightReportStatus.Pending,
                    Source = dto.Source,
                    ReceivedAt = dto.Timestamp,
                    Metadata = dto.Metadata,
                    CreatedAt = DateTime.UtcNow
                };

                // Otomatik onay kontrolü (eşik değerin altında mı?)
                if (overageGrams > 0 && overageGrams <= _autoApproveThresholdGrams)
                {
                    _logger.LogInformation($"Ağırlık farkı eşik değerin altında ({overageGrams}g <= {_autoApproveThresholdGrams}g), otomatik onaylanıyor.");
                    weightReport.Status = WeightReportStatus.AutoApproved;
                    weightReport.ProcessedAt = DateTimeOffset.UtcNow;
                    
                    // Otomatik tahsilat başlat
                    // NOT: Bu işlem background job'da asenkron yapılmalı
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

                // TODO: Gerçek API geldiğinde ödeme servisi entegrasyonu yapılacak
                // Şimdilik mock implementation
                _logger.LogWarning($"Ödeme servisi henüz entegre edilmedi. Rapor ID: {reportId}, Tutar: {report.OverageAmount} {report.Currency}");
                
                // Mock başarılı ödeme
                report.Status = WeightReportStatus.Charged;
                report.ProcessedAt = DateTimeOffset.UtcNow;
                report.PaymentAttemptId = $"MOCK_{Guid.NewGuid():N}";
                report.UpdatedAt = DateTime.UtcNow;

                await _weightReportRepository.UpdateAsync(report);

                _logger.LogInformation($"Fazla tutar tahsil edildi (MOCK): {reportId}, Tutar: {report.OverageAmount} {report.Currency}");
                return true;

                /* Gerçek API geldiğinde:
                try
                {
                    var paymentResult = await _paymentService.ChargeOffSessionAsync(
                        report.Order.UserId.Value,
                        report.OverageAmount,
                        report.Currency,
                        $"Ağırlık farkı ödemesi - Sipariş: {report.Order.OrderNumber}"
                    );

                    if (paymentResult.Success)
                    {
                        report.Status = WeightReportStatus.Charged;
                        report.PaymentAttemptId = paymentResult.TransactionId;
                    }
                    else
                    {
                        report.Status = WeightReportStatus.Failed;
                        report.AdminNote = $"Ödeme hatası: {paymentResult.ErrorMessage}";
                    }

                    report.ProcessedAt = DateTimeOffset.UtcNow;
                    report.UpdatedAt = DateTime.UtcNow;
                    await _weightReportRepository.UpdateAsync(report);

                    return paymentResult.Success;
                }
                catch (Exception paymentEx)
                {
                    _logger.LogError(paymentEx, $"Ödeme işlemi başarısız: {reportId}");
                    report.Status = WeightReportStatus.Failed;
                    report.AdminNote = $"Ödeme hatası: {paymentEx.Message}";
                    await _weightReportRepository.UpdateAsync(report);
                    return false;
                }
                */
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
    }
}
