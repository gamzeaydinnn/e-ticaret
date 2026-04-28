namespace ECommerce.Core.Exceptions
{
    /// <summary>
    /// Mikro senkronizasyon hata hiyerarşisi.
    /// 
    /// NEDEN: Farklı hata tiplerini ayrıştırarak akıllı retry kararları verilebilir.
    /// RetryService.IsRetryableException string bazlı kontrol yerine tip bazlı karar verir.
    /// 
    /// HİYERARŞİ:
    /// MikroSyncException (abstract base)
    ///   ├── MikroApiException → HTTP API hataları (retryable: 5xx, non-retryable: 4xx)
    ///   ├── MikroSqlException → Direct DB hataları (retryable: transient)
    ///   ├── MikroCircuitOpenException → Circuit breaker açık (retry ile geçilemez, beklenmeli)
    ///   ├── MikroConflictException → Çakışma tespit edildi (audit log gerektirir)
    ///   └── MikroSyncTimeoutException → Timeout aşıldı (retryable)
    /// </summary>
    public abstract class MikroSyncException : Exception
    {
        /// <summary>Hatanın retry edilip edilemeyeceğini belirtir</summary>
        public bool IsRetryable { get; }

        /// <summary>İlgili stok kodu (varsa)</summary>
        public string? StokKod { get; init; }

        /// <summary>Sync yönü (FromERP / ToERP)</summary>
        public string? Direction { get; init; }

        protected MikroSyncException(string message, bool isRetryable, Exception? inner = null)
            : base(message, inner)
        {
            IsRetryable = isRetryable;
        }
    }

    /// <summary>
    /// Mikro HTTP API hataları — StatusCode ile retry kararı.
    /// 5xx → Retryable, 4xx → Non-retryable.
    /// </summary>
    public class MikroApiException : MikroSyncException
    {
        public int? StatusCode { get; }
        public string? Endpoint { get; init; }

        public MikroApiException(string message, int? statusCode, Exception? inner = null)
            : base(message, isRetryable: statusCode is null or >= 500, inner)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Mikro Direct SQL bağlantı hataları.
    /// Transient (deadlock/timeout) → Retryable, diğer → Non-retryable.
    /// </summary>
    public class MikroSqlException : MikroSyncException
    {
        public int? SqlErrorNumber { get; }

        /// <summary>Transient SQL hata numaraları (retry edilebilir)</summary>
        private static readonly HashSet<int> TransientSqlErrors = new()
        {
            -2,    // Timeout
            1205,  // Deadlock
            40613, // DB unavailable
            40197, // Service error
            40501, // Service busy
            49918  // Not enough resources
        };

        public MikroSqlException(string message, int? sqlErrorNumber = null, Exception? inner = null)
            : base(message, isRetryable: sqlErrorNumber.HasValue && TransientSqlErrors.Contains(sqlErrorNumber.Value), inner)
        {
            SqlErrorNumber = sqlErrorNumber;
        }
    }

    /// <summary>
    /// Circuit breaker açık — Mikro erişilemez durumda.
    /// Retry ile geçilemez; breaker süresi dolana kadar beklenmeli.
    /// </summary>
    public class MikroCircuitOpenException : MikroSyncException
    {
        /// <summary>Circuit breaker'ın tahmini açılma süresi</summary>
        public TimeSpan? EstimatedRecoveryTime { get; init; }

        public MikroCircuitOpenException(string message, TimeSpan? estimatedRecovery = null)
            : base(message, isRetryable: false)
        {
            EstimatedRecoveryTime = estimatedRecovery;
        }
    }

    /// <summary>
    /// Çakışma tespit edildi — SyncConflictCoordinator tarafından çözülür.
    /// Non-retryable: çözüm kararı verilmeli, otomatik retry anlamsız.
    /// </summary>
    public class MikroConflictException : MikroSyncException
    {
        public string? ConflictType { get; init; }  // Stock / Price / Info
        public string? Strategy { get; init; }       // MikroWins / Conservative_Min / ECommerceWins

        public MikroConflictException(string message)
            : base(message, isRetryable: false)
        { }
    }

    /// <summary>
    /// Sync işlemi zaman aşımına uğradı — retryable.
    /// Polly TimeoutRejectedException yerine domain-specific exception.
    /// </summary>
    public class MikroSyncTimeoutException : MikroSyncException
    {
        public TimeSpan TimeoutDuration { get; }

        public MikroSyncTimeoutException(string message, TimeSpan timeoutDuration, Exception? inner = null)
            : base(message, isRetryable: true, inner)
        {
            TimeoutDuration = timeoutDuration;
        }
    }
}
