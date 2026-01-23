// ═══════════════════════════════════════════════════════════════════════════════
// POSNET İŞLEM LOG SERVİSİ
// POSNET işlemlerinin veritabanına loglanmasını sağlar
// Audit trail, debugging ve mutabakat için kritik
// ═══════════════════════════════════════════════════════════════════════════════

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECommerce.Infrastructure.Services.Payment.Posnet
{
    /// <summary>
    /// POSNET işlem log servisi interface
    /// </summary>
    public interface IPosnetTransactionLogService
    {
        /// <summary>
        /// Yeni işlem logu oluşturur
        /// </summary>
        Task<PosnetTransactionLog> CreateLogAsync(
            string transactionType,
            int? orderId = null,
            int? paymentId = null,
            string? correlationId = null);

        /// <summary>
        /// İstek bilgilerini günceller (XML gönderilmeden önce)
        /// </summary>
        Task UpdateRequestAsync(
            long logId,
            string requestXml,
            string requestUrl,
            string? cardNumber = null);

        /// <summary>
        /// Yanıt bilgilerini günceller (XML alındıktan sonra)
        /// </summary>
        Task UpdateResponseAsync(
            long logId,
            string responseXml,
            bool isSuccess,
            string? approvedCode = null,
            string? errorCode = null,
            string? errorMessage = null,
            string? hostLogKey = null,
            string? authCode = null,
            long? elapsedMs = null);

        /// <summary>
        /// 3D Secure bilgilerini günceller
        /// </summary>
        Task Update3DSecureInfoAsync(
            long logId,
            string mdStatus,
            string? eci = null,
            string? cavv = null);

        /// <summary>
        /// Log kaydını tamamlar
        /// </summary>
        Task CompleteLogAsync(
            long logId,
            int? paymentId = null,
            string? notes = null);

        /// <summary>
        /// Korelasyon ID ile log bulur
        /// </summary>
        Task<PosnetTransactionLog?> GetByCorrelationIdAsync(string correlationId);

        /// <summary>
        /// Sipariş ID'ye göre logları listeler
        /// </summary>
        Task<PosnetTransactionLog[]> GetByOrderIdAsync(int orderId);
    }

    /// <summary>
    /// POSNET işlem log servisi implementasyonu
    /// </summary>
    public class PosnetTransactionLogService : IPosnetTransactionLogService
    {
        private readonly ECommerceDbContext _dbContext;
        private readonly ILogger<PosnetTransactionLogService> _logger;

        // Kart numarası maskeleme regex'i - Ortadaki rakamları * ile değiştirir
        private static readonly Regex CardNumberRegex = new(
            @"<ccno>(\d{6})(\d+)(\d{4})</ccno>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // CVV maskeleme regex'i
        private static readonly Regex CvvRegex = new(
            @"<cvc>(\d+)</cvc>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public PosnetTransactionLogService(
            ECommerceDbContext dbContext,
            ILogger<PosnetTransactionLogService> logger)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public async Task<PosnetTransactionLog> CreateLogAsync(
            string transactionType,
            int? orderId = null,
            int? paymentId = null,
            string? correlationId = null)
        {
            var log = new PosnetTransactionLog
            {
                TransactionType = transactionType,
                OrderId = orderId,
                PaymentId = paymentId,
                CorrelationId = correlationId ?? Guid.NewGuid().ToString("N")[..12],
                CreatedAt = DateTime.UtcNow,
                RequestSentAt = DateTime.UtcNow
            };

            _dbContext.PosnetTransactionLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("[POSNET-LOG] Log oluşturuldu - ID: {LogId}, Type: {Type}, CorrelationId: {CorrelationId}",
                log.Id, transactionType, log.CorrelationId);

            return log;
        }

        /// <inheritdoc/>
        public async Task UpdateRequestAsync(
            long logId,
            string requestXml,
            string requestUrl,
            string? cardNumber = null)
        {
            var log = await _dbContext.PosnetTransactionLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("[POSNET-LOG] Log bulunamadı - ID: {LogId}", logId);
                return;
            }

            // Kart numarası ve CVV'yi maskele (PCI DSS uyumu)
            log.RequestXml = MaskSensitiveData(requestXml);
            log.RequestUrl = requestUrl;

            // Kart bilgilerini çıkar (maskelenmiş)
            if (!string.IsNullOrEmpty(cardNumber) && cardNumber.Length >= 10)
            {
                log.CardBin = cardNumber[..6];
                log.CardLastFour = cardNumber[^4..];
                log.CardType = DetectCardType(cardNumber);
                log.CardHash = HashCardNumber(cardNumber);
            }

            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task UpdateResponseAsync(
            long logId,
            string responseXml,
            bool isSuccess,
            string? approvedCode = null,
            string? errorCode = null,
            string? errorMessage = null,
            string? hostLogKey = null,
            string? authCode = null,
            long? elapsedMs = null)
        {
            var log = await _dbContext.PosnetTransactionLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("[POSNET-LOG] Log bulunamadı - ID: {LogId}", logId);
                return;
            }

            log.ResponseXml = responseXml;
            log.ResponseReceivedAt = DateTime.UtcNow;
            log.IsSuccess = isSuccess;
            log.ApprovedCode = approvedCode;
            log.ErrorCode = errorCode;
            log.ErrorMessage = errorMessage?.Length > 500 ? errorMessage[..500] : errorMessage;
            log.HostLogKey = hostLogKey;
            log.AuthCode = authCode;
            log.ElapsedMilliseconds = elapsedMs;

            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("[POSNET-LOG] Response güncellendi - ID: {LogId}, Success: {Success}, HostLogKey: {HostLogKey}",
                logId, isSuccess, hostLogKey);
        }

        /// <inheritdoc/>
        public async Task Update3DSecureInfoAsync(
            long logId,
            string mdStatus,
            string? eci = null,
            string? cavv = null)
        {
            var log = await _dbContext.PosnetTransactionLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("[POSNET-LOG] Log bulunamadı - ID: {LogId}", logId);
                return;
            }

            log.Is3DSecure = true;
            log.MdStatus = mdStatus;
            log.Eci = eci;
            log.Cavv = cavv;

            await _dbContext.SaveChangesAsync();
        }

        /// <inheritdoc/>
        public async Task CompleteLogAsync(
            long logId,
            int? paymentId = null,
            string? notes = null)
        {
            var log = await _dbContext.PosnetTransactionLogs.FindAsync(logId);
            if (log == null)
            {
                _logger.LogWarning("[POSNET-LOG] Log bulunamadı - ID: {LogId}", logId);
                return;
            }

            if (paymentId.HasValue)
            {
                log.PaymentId = paymentId.Value;
            }

            if (!string.IsNullOrEmpty(notes))
            {
                log.Notes = (log.Notes ?? "") + "\n" + notes;
            }

            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("[POSNET-LOG] Log tamamlandı - ID: {LogId}, PaymentId: {PaymentId}, Success: {Success}",
                logId, paymentId, log.IsSuccess);
        }

        /// <inheritdoc/>
        public async Task<PosnetTransactionLog?> GetByCorrelationIdAsync(string correlationId)
        {
            return await _dbContext.PosnetTransactionLogs
                .FirstOrDefaultAsync(l => l.CorrelationId == correlationId);
        }

        /// <inheritdoc/>
        public async Task<PosnetTransactionLog[]> GetByOrderIdAsync(int orderId)
        {
            return await _dbContext.PosnetTransactionLogs
                .Where(l => l.OrderId == orderId)
                .OrderByDescending(l => l.CreatedAt)
                .ToArrayAsync();
        }

        // ═══════════════════════════════════════════════════════════════════════════════
        // PRİVATE HELPER METODLAR
        // ═══════════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Hassas verileri maskeler (PCI DSS uyumu)
        /// Kart numarası: ilk 6 ve son 4 hane görünür, ortası *
        /// CVV: tamamen maskelenir
        /// </summary>
        private static string MaskSensitiveData(string xml)
        {
            if (string.IsNullOrEmpty(xml)) return xml;

            // Kart numarasını maskele: 411111******1111
            xml = CardNumberRegex.Replace(xml, m =>
            {
                var bin = m.Groups[1].Value; // İlk 6 hane
                var lastFour = m.Groups[3].Value; // Son 4 hane
                var middle = new string('*', m.Groups[2].Value.Length);
                return $"<ccno>{bin}{middle}{lastFour}</ccno>";
            });

            // CVV'yi tamamen maskele
            xml = CvvRegex.Replace(xml, "<cvc>***</cvc>");

            return xml;
        }

        /// <summary>
        /// Kart tipini tespit eder (BIN'e göre)
        /// </summary>
        private static string DetectCardType(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 2)
                return "Unknown";

            // Visa: 4 ile başlar
            if (cardNumber.StartsWith("4"))
                return "Visa";

            // MasterCard: 51-55 veya 2221-2720
            if (cardNumber.Length >= 2)
            {
                var firstTwo = int.Parse(cardNumber[..2]);
                if (firstTwo >= 51 && firstTwo <= 55)
                    return "MasterCard";

                if (cardNumber.Length >= 4)
                {
                    var firstFour = int.Parse(cardNumber[..4]);
                    if (firstFour >= 2221 && firstFour <= 2720)
                        return "MasterCard";
                }
            }

            // Troy: 9 ile başlar (Türkiye)
            if (cardNumber.StartsWith("9"))
                return "Troy";

            // Amex: 34 veya 37
            if (cardNumber.StartsWith("34") || cardNumber.StartsWith("37"))
                return "Amex";

            return "Other";
        }

        /// <summary>
        /// Kart numarasını hash'ler (karşılaştırma için)
        /// </summary>
        private static string HashCardNumber(string cardNumber)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(cardNumber));
            return Convert.ToHexString(bytes)[..32]; // İlk 32 karakter
        }
    }
}
