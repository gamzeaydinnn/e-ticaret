using System.Diagnostics;
using ECommerce.Core.DTOs.Micro;
using ECommerce.Core.Interfaces;
using ECommerce.Core.Interfaces.Sync;
using ECommerce.Entities.Concrete;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Sync
{
    /// <summary>
    /// Cari (Müşteri) senkronizasyon servisi - E-ticaret müşterilerini Mikro ERP'ye aktarır.
    /// 
    /// NEDEN: Sipariş Mikro'ya aktarılmadan önce müşterinin cari kaydı olmalı.
    /// Bu servis e-ticaret kullanıcılarını Mikro cari hesaplarına dönüştürür.
    /// 
    /// AKIŞ:
    /// 1. Yeni müşteri sipariş verir
    /// 2. Bu servis müşteriyi Mikro formatına dönüştürür
    /// 3. MikroAPI CariKaydetV2 endpoint'ine gönderir
    /// 4. Dönen cari kodu e-ticaret'te saklanır
    /// 5. Artık siparişler bu cari koduna bağlanabilir
    /// 
    /// ÖNEMLİ: Cari kodu formatı "ETCMUST{UserId}" şeklinde
    /// standardize edilmeli, böylece çift yönlü eşleştirme kolay olur.
    /// </summary>
    public class CariSyncService : ICariSyncService
    {
        // ==================== BAĞIMLILIKLAR ====================

        private readonly IMicroService _microService;
        private readonly IUserRepository _userRepository;
        private readonly IMikroSyncRepository _syncRepository;
        private readonly ILogger<CariSyncService> _logger;

        // Sabitler
        private const string SYNC_TYPE = "Cari";
        private const string DIRECTION_TO_ERP = "ToERP";
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const string CARI_PREFIX = "ETCMUST"; // E-ticaret müşteri prefix
        private const string CARI_MISAFIR_PREFIX = "ETCMIS"; // Misafir müşteri prefix
        private const string ONLINE_BOLGE_KODU = "ONLINE"; // Online müşteri grubu

        // ==================== CONSTRUCTOR ====================

        public CariSyncService(
            IMicroService microService,
            IUserRepository userRepository,
            IMikroSyncRepository syncRepository,
            ILogger<CariSyncService> logger)
        {
            _microService = microService ?? throw new ArgumentNullException(nameof(microService));
            _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            _syncRepository = syncRepository ?? throw new ArgumentNullException(nameof(syncRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ==================== CARİ OLUŞTURMA / GÜNCELLEME ====================

        /// <inheritdoc />
        public async Task<SyncResult> CreateOrUpdateCariAsync(
            int? userId,
            string customerName,
            string email,
            string phone,
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var cariKod = GenerateCariKod(userId);

            _logger.LogInformation(
                "[CariSyncService] Cari oluşturma/güncelleme başlatıldı. " +
                "UserId: {UserId}, CariKod: {CariKod}",
                userId, cariKod);

            try
            {
                // 1. Mikro cari DTO oluştur
                var cariDto = new MikroCariKaydetRequestDto
                {
                    CariKod = cariKod,
                    CariUnvan1 = customerName,
                    CariHareketTipi = 0, // Müşteri
                    CariBolgeKodu = ONLINE_BOLGE_KODU,
                    CariEmail = email,
                    CariCepTel = FormatPhoneNumber(phone),
                    CariKisiKurumFlg = 0, // Şahıs
                    CariOzelKod = userId?.ToString(), // E-ticaret referansı
                    CariAciklama = $"E-ticaret müşterisi. Oluşturma: {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                    CariPasifFl = false
                };

                // 2. Mikro'ya gönder (retry ile)
                var result = await PushCariWithRetryAsync(cariDto, userId, cancellationToken);

                stopwatch.Stop();

                if (result.IsSuccess)
                {
                    await _syncRepository.UpdateSyncSuccessAsync(
                        SYNC_TYPE,
                        DIRECTION_TO_ERP,
                        1,
                        stopwatch.ElapsedMilliseconds,
                        cancellationToken);

                    _logger.LogInformation(
                        "[CariSyncService] Cari başarıyla oluşturuldu/güncellendi. " +
                        "CariKod: {CariKod}, Süre: {Duration}ms",
                        cariKod, stopwatch.ElapsedMilliseconds);
                }

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _syncRepository.UpdateSyncFailureAsync(
                    SYNC_TYPE,
                    DIRECTION_TO_ERP,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(ex,
                    "[CariSyncService] Cari oluşturma/güncelleme başarısız. " +
                    "UserId: {UserId}",
                    userId);

                return SyncResult.Fail(new SyncError(
                    "CreateOrUpdateCari",
                    userId?.ToString(),
                    ex.Message));
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> SyncUserToCariAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            _logger.LogInformation(
                "[CariSyncService] Kullanıcı cari senkronizasyonu başlatıldı. UserId: {UserId}",
                userId);

            try
            {
                // 1. Kullanıcıyı bul
                var user = await _userRepository.GetByIdAsync(userId);

                if (user == null)
                {
                    var error = new SyncError(
                        "SyncUserToCari",
                        userId.ToString(),
                        "Kullanıcı bulunamadı");

                    _logger.LogWarning(
                        "[CariSyncService] Kullanıcı bulunamadı. UserId: {UserId}",
                        userId);

                    return SyncResult.Fail(error);
                }

                // 2. Cari oluştur/güncelle
                var fullName = GetUserFullName(user);
                
                return await CreateOrUpdateCariAsync(
                    userId,
                    fullName,
                    user.Email ?? "",
                    user.PhoneNumber ?? "",
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CariSyncService] Kullanıcı senkronizasyonu başarısız. UserId: {UserId}",
                    userId);

                return SyncResult.Fail(new SyncError(
                    "SyncUserToCari",
                    userId.ToString(),
                    ex.Message));
            }
        }

        // ==================== CARİ SORGULAMA ====================

        /// <inheritdoc />
        public async Task<string?> GetMikroCariKodAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // 1. Önce sync log'dan kontrol et
                var log = await _syncRepository.GetPendingLogsAsync("Cari", 10, cancellationToken);
                var successLog = log.FirstOrDefault(l => 
                    l.InternalId == userId.ToString() && 
                    l.Status == "Success");

                if (successLog != null && !string.IsNullOrEmpty(successLog.ExternalId))
                {
                    return successLog.ExternalId;
                }

                // 2. Varsayılan cari kodu üret ve kontrol et
                // NOT: Gerçek implementasyonda Mikro API'den sorgulanabilir
                var expectedCariKod = GenerateCariKod(userId);

                _logger.LogDebug(
                    "[CariSyncService] Cari kod sorgulandı. UserId: {UserId}, CariKod: {CariKod}",
                    userId, expectedCariKod);

                // Şimdilik varsayılan kodu döndür
                // Gerçek implementasyonda Mikro'dan doğrulama yapılabilir
                return expectedCariKod;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[CariSyncService] Cari kod sorgulama hatası. UserId: {UserId}",
                    userId);

                return null;
            }
        }

        /// <inheritdoc />
        public async Task<SyncResult> SyncAllUsersToCariAsync(
            CancellationToken cancellationToken = default)
        {
            var stopwatch = Stopwatch.StartNew();
            var errors = new List<SyncError>();
            int successCount = 0;
            int failedCount = 0;

            _logger.LogInformation(
                "[CariSyncService] Toplu kullanıcı-cari senkronizasyonu başlatıldı");

            try
            {
                // 1. Tüm aktif kullanıcıları al
                var users = await _userRepository.GetAllAsync();
                var userList = users.ToList();

                _logger.LogInformation(
                    "[CariSyncService] {Count} kullanıcı işlenecek",
                    userList.Count);

                // 2. Her kullanıcı için cari oluştur
                foreach (var user in userList)
                {
                    try
                    {
                        var result = await SyncUserToCariAsync(user.Id, cancellationToken);

                        if (result.IsSuccess)
                            successCount++;
                        else
                        {
                            failedCount++;
                            errors.AddRange(result.Errors);
                        }
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        errors.Add(new SyncError(
                            "SyncUserToCari",
                            user.Id.ToString(),
                            ex.Message));
                    }
                }

                stopwatch.Stop();

                await _syncRepository.UpdateSyncSuccessAsync(
                    SYNC_TYPE,
                    DIRECTION_TO_ERP,
                    successCount,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                _logger.LogInformation(
                    "[CariSyncService] Toplu senkronizasyon tamamlandı. " +
                    "Başarılı: {Success}, Hatalı: {Failed}, Süre: {Duration}ms",
                    successCount, failedCount, stopwatch.ElapsedMilliseconds);

                return SyncResult.Ok(successCount, errors);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                await _syncRepository.UpdateSyncFailureAsync(
                    SYNC_TYPE,
                    DIRECTION_TO_ERP,
                    ex.Message,
                    cancellationToken);

                _logger.LogError(ex,
                    "[CariSyncService] Toplu senkronizasyon başarısız!");

                return SyncResult.Fail(new SyncError(
                    "SyncAllUsersToCari",
                    null,
                    ex.Message));
            }
        }

        // ==================== YARDIMCI METODLAR ====================

        /// <summary>
        /// Cari kaydını Mikro'ya retry mekanizması ile gönderir.
        /// </summary>
        private async Task<SyncResult> PushCariWithRetryAsync(
            MikroCariKaydetRequestDto cariDto,
            int? userId,
            CancellationToken cancellationToken)
        {
            var syncLog = new MicroSyncLog
            {
                EntityType = "Cari",
                Direction = DIRECTION_TO_ERP,
                InternalId = userId?.ToString(),
                ExternalId = cariDto.CariKod,
                Status = "Pending",
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            for (int attempt = 1; attempt <= MAX_RETRY_ATTEMPTS; attempt++)
            {
                try
                {
                    syncLog.Attempts = attempt;
                    syncLog.LastAttemptAt = DateTime.UtcNow;

                    // MicroService.UpsertCustomersAsync kullan
                    // NOT: MicroCustomerDto yapısına uygun mapping
                    var customerDto = new MicroCustomerDto
                    {
                        ExternalId = cariDto.CariKod,
                        FullName = cariDto.CariUnvan1,
                        Email = cariDto.CariEmail ?? "",
                        Phone = cariDto.CariCepTel
                    };

                    var success = await _microService.UpsertCustomersAsync(new[] { customerDto });

                    if (success)
                    {
                        syncLog.Status = "Success";
                        syncLog.Message = $"Cari oluşturuldu: {cariDto.CariKod}";
                        await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

                        _logger.LogDebug(
                            "[CariSyncService] Cari Mikro'ya gönderildi. CariKod: {CariKod}",
                            cariDto.CariKod);

                        return SyncResult.Ok(1);
                    }
                    else
                    {
                        throw new InvalidOperationException("MikroAPI false döndürdü");
                    }
                }
                catch (Exception ex)
                {
                    syncLog.LastError = ex.Message;

                    _logger.LogWarning(
                        "[CariSyncService] Cari gönderimi başarısız. " +
                        "CariKod: {CariKod}, Deneme: {Attempt}/{Max}, Hata: {Error}",
                        cariDto.CariKod, attempt, MAX_RETRY_ATTEMPTS, ex.Message);

                    if (attempt < MAX_RETRY_ATTEMPTS)
                    {
                        var delay = TimeSpan.FromSeconds(Math.Pow(2, attempt - 1));
                        await Task.Delay(delay, cancellationToken);
                    }
                }
            }

            // Max deneme aşıldı
            syncLog.Status = "Failed";
            syncLog.Message = $"Max {MAX_RETRY_ATTEMPTS} deneme aşıldı";
            await _syncRepository.CreateLogAsync(syncLog, cancellationToken);

            _logger.LogError(
                "[CariSyncService] Cari gönderimi başarısız (max deneme). CariKod: {CariKod}",
                cariDto.CariKod);

            return SyncResult.Fail(new SyncError(
                "PushCari",
                cariDto.CariKod,
                syncLog.LastError ?? "Max retry exceeded"));
        }

        /// <summary>
        /// UserId'ye göre benzersiz cari kodu üretir.
        /// Format: ETCMUST{UserId:D6} (6 haneli, sıfır dolgulu)
        /// Örnek: ETCMUST000042
        /// </summary>
        private string GenerateCariKod(int? userId)
        {
            if (userId.HasValue)
                return $"{CARI_PREFIX}{userId.Value:D6}";

            // Misafir müşteri için timestamp bazlı
            return $"{CARI_MISAFIR_PREFIX}{DateTime.UtcNow:yyyyMMddHHmmss}";
        }

        /// <summary>
        /// Telefon numarasını Mikro formatına çevirir.
        /// Format: 05XXXXXXXXX
        /// </summary>
        private string? FormatPhoneNumber(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return null;

            // Sadece rakamları al
            var digits = new string(phone.Where(char.IsDigit).ToArray());

            // 90 ile başlıyorsa kaldır
            if (digits.StartsWith("90") && digits.Length == 12)
                digits = "0" + digits.Substring(2);

            // 0 ile başlamıyorsa ekle
            if (!digits.StartsWith("0") && digits.Length == 10)
                digits = "0" + digits;

            return digits;
        }

        /// <summary>
        /// Kullanıcının tam adını oluşturur.
        /// </summary>
        private string GetUserFullName(User user)
        {
            var parts = new List<string>();

            if (!string.IsNullOrWhiteSpace(user.FirstName))
                parts.Add(user.FirstName);

            if (!string.IsNullOrWhiteSpace(user.LastName))
                parts.Add(user.LastName);

            if (parts.Count == 0)
                return user.Email ?? "Müşteri";

            return string.Join(" ", parts);
        }
    }
}
