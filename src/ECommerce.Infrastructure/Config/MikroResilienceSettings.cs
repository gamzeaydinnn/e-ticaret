namespace ECommerce.Infrastructure.Config
{
    /// <summary>
    /// Mikro API iletişimi için Polly dayanıklılık ayarları.
    /// appsettings.json → "MikroResilience" bölümünden bind edilir.
    /// 
    /// NEDEN AYRI SINIF: MikroSettings zaten kalabalık; resilience ayarları
    /// bağımsız tuning gerektirir (CB threshold, retry delay, timeout).
    /// </summary>
    public class MikroResilienceSettings
    {
        // ════════════════════ HTTP (Mikro HTTP API) ════════════════════

        /// <summary>Retry denemesi sayısı (ilk istek hariç)</summary>
        public int HttpMaxRetryAttempts { get; set; } = 3;

        /// <summary>İlk retry bekleme süresi (ms) — exponential backoff base</summary>
        public int HttpRetryBaseDelayMs { get; set; } = 500;

        /// <summary>Tek istek timeout (sn) — her deneme için ayrı ayrı uygulanır</summary>
        public int HttpPerAttemptTimeoutSeconds { get; set; } = 30;

        /// <summary>Tüm retry dahil toplam timeout (sn)</summary>
        public int HttpTotalTimeoutSeconds { get; set; } = 120;

        /// <summary>Circuit breaker açılma eşiği (oran, 0.0-1.0)</summary>
        public double HttpCircuitBreakerFailureThreshold { get; set; } = 0.5;

        /// <summary>CB hata oranı hesaplama penceresi (sn)</summary>
        public int HttpCircuitBreakerSamplingDurationSeconds { get; set; } = 30;

        /// <summary>CB penceresi içinde minimum istek sayısı (threshold aktifleşmesi için)</summary>
        public int HttpCircuitBreakerMinimumThroughput { get; set; } = 5;

        /// <summary>CB açık kaldığı süre (sn) — bu süre sonunda half-open'a geçer</summary>
        public int HttpCircuitBreakerBreakDurationSeconds { get; set; } = 30;

        // ════════════════════ SQL (Mikro Direct DB) ════════════════════

        /// <summary>SQL retry denemesi sayısı</summary>
        public int SqlMaxRetryAttempts { get; set; } = 2;

        /// <summary>SQL retry base delay (ms)</summary>
        public int SqlRetryBaseDelayMs { get; set; } = 300;

        /// <summary>SQL toplam timeout (sn)</summary>
        public int SqlTotalTimeoutSeconds { get; set; } = 60;

        /// <summary>SQL CB açılma eşiği</summary>
        public double SqlCircuitBreakerFailureThreshold { get; set; } = 0.5;

        /// <summary>SQL CB sampling penceresi (sn)</summary>
        public int SqlCircuitBreakerSamplingDurationSeconds { get; set; } = 60;

        /// <summary>SQL CB minimum throughput</summary>
        public int SqlCircuitBreakerMinimumThroughput { get; set; } = 3;

        /// <summary>SQL CB break süresi (sn)</summary>
        public int SqlCircuitBreakerBreakDurationSeconds { get; set; } = 45;
    }
}
