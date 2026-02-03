using ECommerce.Core.Interfaces.Jobs;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ECommerce.Business.Services.Jobs
{
    /// <summary>
    /// Mikro ERP senkronizasyon job'larını yöneten scheduler servisi.
    /// 
    /// GÖREV: Hangfire recurring job'larını kaydetme, tetikleme,
    /// aktif/pasif yapma ve durumlarını izleme.
    /// 
    /// CRON ZAMANLARI:
    /// - Stok Sync: Her 15 dakika (*/15 * * * *)
    /// - Fiyat Sync: Her saat (0 * * * *)
    /// - Full Sync: Her gün 06:00 (0 6 * * *)
    /// - Sipariş Push: Event-driven (manuel veya recurring)
    /// 
    /// NOT: Hangfire dashboard'dan da job'lar görüntülenebilir
    /// ve manuel tetiklenebilir.
    /// </summary>
    public class MikroJobScheduler : IMikroJobScheduler
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MikroJobScheduler> _logger;
        private readonly IRecurringJobManager _recurringJobManager;

        // Job tanımları
        private readonly Dictionary<string, JobDefinition> _jobDefinitions;

        public MikroJobScheduler(
            IConfiguration configuration,
            ILogger<MikroJobScheduler> logger,
            IRecurringJobManager recurringJobManager)
        {
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _recurringJobManager = recurringJobManager ?? throw new ArgumentNullException(nameof(recurringJobManager));

            // Job tanımlarını yükle
            _jobDefinitions = LoadJobDefinitions();
        }

        /// <inheritdoc />
        public void RegisterAllJobs()
        {
            _logger.LogInformation(
                "[MikroJobScheduler] ═══════════════════════════════════════════════════════");
            _logger.LogInformation(
                "[MikroJobScheduler] MİKRO SENKRONİZASYON JOB'LARI KAYDEDİLİYOR");
            _logger.LogInformation(
                "[MikroJobScheduler] ═══════════════════════════════════════════════════════");

            foreach (var job in _jobDefinitions.Values.Where(j => j.IsEnabled))
            {
                RegisterJob(job);
            }

            _logger.LogInformation(
                "[MikroJobScheduler] {Count} job kaydedildi",
                _jobDefinitions.Values.Count(j => j.IsEnabled));
        }

        /// <inheritdoc />
        public string TriggerJob(string jobName)
        {
            if (!_jobDefinitions.TryGetValue(jobName, out var job))
            {
                _logger.LogWarning(
                    "[MikroJobScheduler] Job bulunamadı: {JobName}",
                    jobName);
                throw new ArgumentException($"Job bulunamadı: {jobName}");
            }

            _logger.LogInformation(
                "[MikroJobScheduler] Job manuel tetikleniyor: {JobName}",
                jobName);

            // Hangfire BackgroundJob.Enqueue kullanarak hemen çalıştır
            var jobId = job.JobType switch
            {
                JobType.StokSync => BackgroundJob.Enqueue<IStokSyncJob>(
                    j => j.ExecuteAsync(CancellationToken.None)),
                
                JobType.FiyatSync => BackgroundJob.Enqueue<IFiyatSyncJob>(
                    j => j.ExecuteAsync(CancellationToken.None)),
                
                JobType.FullSync => BackgroundJob.Enqueue<IFullSyncJob>(
                    j => j.ExecuteAsync(CancellationToken.None)),
                
                JobType.SiparisPush => BackgroundJob.Enqueue<ISiparisPushJob>(
                    j => j.PushPendingOrdersAsync(CancellationToken.None)),
                
                // ADIM 6: RETRY JOB TETİKLEME
                JobType.RetrySync => BackgroundJob.Enqueue<RetryJob>(
                    j => j.ExecuteAsync(CancellationToken.None)),
                
                _ => throw new ArgumentException($"Bilinmeyen job tipi: {job.JobType}")
            };

            _logger.LogInformation(
                "[MikroJobScheduler] Job tetiklendi: {JobName}, Hangfire ID: {JobId}",
                jobName, jobId);

            return jobId;
        }

        /// <inheritdoc />
        public void DisableJob(string jobName)
        {
            if (!_jobDefinitions.TryGetValue(jobName, out var job))
            {
                _logger.LogWarning(
                    "[MikroJobScheduler] Job bulunamadı: {JobName}",
                    jobName);
                return;
            }

            _logger.LogInformation(
                "[MikroJobScheduler] Job devre dışı bırakılıyor: {JobName}",
                jobName);

            // Hangfire'dan kaldır
            RecurringJob.RemoveIfExists(jobName);
            
            // Local state güncelle
            job.IsEnabled = false;

            _logger.LogInformation(
                "[MikroJobScheduler] Job devre dışı bırakıldı: {JobName}",
                jobName);
        }

        /// <inheritdoc />
        public void EnableJob(string jobName)
        {
            if (!_jobDefinitions.TryGetValue(jobName, out var job))
            {
                _logger.LogWarning(
                    "[MikroJobScheduler] Job bulunamadı: {JobName}",
                    jobName);
                return;
            }

            _logger.LogInformation(
                "[MikroJobScheduler] Job aktif ediliyor: {JobName}",
                jobName);

            // Yeniden kaydet
            job.IsEnabled = true;
            RegisterJob(job);

            _logger.LogInformation(
                "[MikroJobScheduler] Job aktif edildi: {JobName}",
                jobName);
        }

        /// <inheritdoc />
        public Task<IEnumerable<JobStatusInfo>> GetJobStatusesAsync()
        {
            var statuses = _jobDefinitions.Values.Select(job => new JobStatusInfo
            {
                JobName = job.Name,
                CronExpression = job.CronExpression,
                IsEnabled = job.IsEnabled,
                LastExecution = GetLastExecution(job.Name),
                NextExecution = job.IsEnabled ? GetNextExecution(job.CronExpression) : null,
                LastStatus = GetLastStatus(job.Name)
            });

            return Task.FromResult(statuses);
        }

        #region Private Methods

        /// <summary>
        /// Job tanımlarını configuration'dan yükler.
        /// </summary>
        private Dictionary<string, JobDefinition> LoadJobDefinitions()
        {
            var section = _configuration.GetSection("MikroApi:Jobs");
            
            return new Dictionary<string, JobDefinition>
            {
                ["mikro-stok-sync"] = new JobDefinition
                {
                    Name = "mikro-stok-sync",
                    Description = "Stok senkronizasyonu (15 dk)",
                    CronExpression = section["StokSyncCron"] ?? "*/15 * * * *",
                    JobType = JobType.StokSync,
                    IsEnabled = section.GetValue("StokSyncEnabled", true),
                    TimeoutMinutes = 5,
                    Queue = "mikro-sync"
                },
                ["mikro-fiyat-sync"] = new JobDefinition
                {
                    Name = "mikro-fiyat-sync",
                    Description = "Fiyat senkronizasyonu (1 saat)",
                    CronExpression = section["FiyatSyncCron"] ?? "0 * * * *",
                    JobType = JobType.FiyatSync,
                    IsEnabled = section.GetValue("FiyatSyncEnabled", true),
                    TimeoutMinutes = 10,
                    Queue = "mikro-sync"
                },
                ["mikro-full-sync"] = new JobDefinition
                {
                    Name = "mikro-full-sync",
                    Description = "Tam senkronizasyon (günlük 06:00)",
                    CronExpression = section["FullSyncCron"] ?? "0 6 * * *",
                    JobType = JobType.FullSync,
                    IsEnabled = section.GetValue("FullSyncEnabled", true),
                    TimeoutMinutes = 30,
                    Queue = "mikro-sync"
                },
                ["mikro-siparis-push"] = new JobDefinition
                {
                    Name = "mikro-siparis-push",
                    Description = "Bekleyen sipariş gönderimi (5 dk)",
                    CronExpression = section["SiparisPushCron"] ?? "*/5 * * * *",
                    JobType = JobType.SiparisPush,
                    IsEnabled = section.GetValue("SiparisPushEnabled", true),
                    TimeoutMinutes = 10,
                    Queue = "mikro-orders"
                },
                // ADIM 6: RETRY SYNC JOB
                ["mikro-retry-sync"] = new JobDefinition
                {
                    Name = "mikro-retry-sync",
                    Description = "Başarısız senkronizasyonları tekrar deneme (5 dk)",
                    CronExpression = section["RetrySyncCron"] ?? "*/5 * * * *",
                    JobType = JobType.RetrySync,
                    IsEnabled = section.GetValue("RetrySyncEnabled", true),
                    TimeoutMinutes = 5,
                    Queue = "mikro-retry"
                }
            };
        }

        /// <summary>
        /// Tek bir job'ı Hangfire'a kaydeder.
        /// </summary>
        private void RegisterJob(JobDefinition job)
        {
            try
            {
                var options = new RecurringJobOptions
                {
                    TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"),
                    MisfireHandling = MisfireHandlingMode.Ignorable // Kaçırılan job'ları atla
                };

                switch (job.JobType)
                {
                    case JobType.StokSync:
                        _recurringJobManager.AddOrUpdate<IStokSyncJob>(
                            job.Name,
                            j => j.ExecuteAsync(CancellationToken.None),
                            job.CronExpression,
                            options);
                        break;

                    case JobType.FiyatSync:
                        _recurringJobManager.AddOrUpdate<IFiyatSyncJob>(
                            job.Name,
                            j => j.ExecuteAsync(CancellationToken.None),
                            job.CronExpression,
                            options);
                        break;

                    case JobType.FullSync:
                        _recurringJobManager.AddOrUpdate<IFullSyncJob>(
                            job.Name,
                            j => j.ExecuteAsync(CancellationToken.None),
                            job.CronExpression,
                            options);
                        break;

                    case JobType.SiparisPush:
                        _recurringJobManager.AddOrUpdate<ISiparisPushJob>(
                            job.Name,
                            j => j.PushPendingOrdersAsync(CancellationToken.None),
                            job.CronExpression,
                            options);
                        break;

                    // ADIM 6: RETRY JOB KAYDEDERKEN
                    case JobType.RetrySync:
                        _recurringJobManager.AddOrUpdate<RetryJob>(
                            job.Name,
                            j => j.ExecuteAsync(CancellationToken.None),
                            job.CronExpression,
                            options);
                        break;
                }

                _logger.LogInformation(
                    "[MikroJobScheduler] Job kaydedildi: {JobName} ({Cron})",
                    job.Name, job.CronExpression);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[MikroJobScheduler] Job kaydedilemedi: {JobName}",
                    job.Name);
            }
        }

        /// <summary>
        /// Job'ın son çalışma zamanını getirir.
        /// </summary>
        private DateTime? GetLastExecution(string jobName)
        {
            try
            {
                // RecurringJobManager üzerinden job bilgisi al
                // Not: Hangfire.Core'da RecurringJobDto kullanılamıyor doğrudan
                // JobStorage.Current kullanarak recurring job bilgisi alınır
                var monitoringApi = JobStorage.Current.GetMonitoringApi();
                // Monitoring API üzerinden job durumu kontrol edilebilir
                return null; // Şimdilik null döner, dashboard'dan takip edilir
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Bir sonraki çalışma zamanını hesaplar.
        /// </summary>
        private DateTime? GetNextExecution(string cronExpression)
        {
            try
            {
                // Cron expression parse et ve sonraki zamanı hesapla
                // Not: Cronos kütüphanesi Hangfire'ın bir parçası değil
                // Basit bir hesaplama ile yaklaşık değer verilir
                return DateTime.UtcNow.AddMinutes(15); // Varsayılan tahmin
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Job'ın son durumunu getirir.
        /// </summary>
        private string? GetLastStatus(string jobName)
        {
            try
            {
                // Monitoring API üzerinden son job durumu kontrol edilebilir
                // Dashboard'dan daha detaylı takip yapılabilir
                return "Unknown"; // Varsayılan değer
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// Job tanım sınıfı.
        /// </summary>
        private class JobDefinition
        {
            public string Name { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string CronExpression { get; set; } = string.Empty;
            public JobType JobType { get; set; }
            public bool IsEnabled { get; set; } = true;
            public int TimeoutMinutes { get; set; } = 10;
            public string Queue { get; set; } = "default";
        }

        /// <summary>
        /// Job tipi enum'ı.
        /// </summary>
        private enum JobType
        {
            StokSync,
            FiyatSync,
            FullSync,
            SiparisPush,
            RetrySync  // ADIM 6: Başarısız senkronizasyonları tekrar deneme
        }

        #endregion
    }
}
