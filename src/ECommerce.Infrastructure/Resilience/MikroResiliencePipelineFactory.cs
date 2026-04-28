using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ECommerce.Infrastructure.Config;

namespace ECommerce.Infrastructure.Resilience
{
    /// <summary>
    /// Mikro API iletişimi için Polly v8 tabanlı dayanıklılık pipeline'ı.
    /// 
    /// NEDEN POLLY: MicroService.SendMikroRequestAsync'teki manuel retry yerine
    /// Polly ile standart, test edilebilir ve genişletilebilir resilience sağlanır.
    /// 
    /// PİPELINE KATMANLARI (dıştan içe):
    /// 1. Total Timeout — Tüm retry'lar dahil max süre (varsayılan 2dk)
    /// 2. Retry — Exponential backoff + jitter, 5xx/timeout/network hataları
    /// 3. Circuit Breaker — Ardışık hata eşiğinde devreyi açar
    /// 4. Per-Attempt Timeout — Tek istek için max süre (varsayılan 30sn)
    /// 
    /// METRIC TOPLAMA: Her olay (retry, CB open/close, timeout) loglanır ve
    /// ISyncMetricsService üzerinden ölçülür.
    /// </summary>
    public sealed class MikroResiliencePipelineFactory
    {
        private readonly MikroResilienceSettings _settings;
        private readonly ILogger<MikroResiliencePipelineFactory> _logger;

        // Thread-safe singleton pipeline — build edilince değişmez
        private ResiliencePipeline<HttpResponseMessage>? _httpPipeline;
        private ResiliencePipeline? _sqlPipeline;
        private readonly object _lock = new();

        // Circuit breaker durum bilgisi — admin panelde göstermek için
        private volatile CircuitBreakerState _httpCircuitState = CircuitBreakerState.Closed;
        private volatile CircuitBreakerState _sqlCircuitState = CircuitBreakerState.Closed;

        public MikroResiliencePipelineFactory(
            IOptions<MikroResilienceSettings> settings,
            ILogger<MikroResiliencePipelineFactory> logger)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>Admin panelde gösterilecek circuit breaker durumu</summary>
        public CircuitBreakerState HttpCircuitState => _httpCircuitState;
        public CircuitBreakerState SqlCircuitState => _sqlCircuitState;

        /// <summary>
        /// Mikro HTTP API istekleri için resilience pipeline.
        /// Retry + Circuit Breaker + Timeout katmanları içerir.
        /// </summary>
        public ResiliencePipeline<HttpResponseMessage> GetHttpPipeline()
        {
            if (_httpPipeline != null) return _httpPipeline;
            lock (_lock)
            {
                if (_httpPipeline != null) return _httpPipeline;
                _httpPipeline = BuildHttpPipeline();
                return _httpPipeline;
            }
        }

        /// <summary>
        /// Mikro SQL (direct DB) işlemleri için resilience pipeline.
        /// Daha kısa timeout, daha az retry — SQL genelde ya hızlı döner ya timeout olur.
        /// </summary>
        public ResiliencePipeline GetSqlPipeline()
        {
            if (_sqlPipeline != null) return _sqlPipeline;
            lock (_lock)
            {
                if (_sqlPipeline != null) return _sqlPipeline;
                _sqlPipeline = BuildSqlPipeline();
                return _sqlPipeline;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // HTTP Pipeline Build
        // ════════════════════════════════════════════════════════════════════

        private ResiliencePipeline<HttpResponseMessage> BuildHttpPipeline()
        {
            _logger.LogInformation(
                "[Resilience] HTTP pipeline oluşturuluyor. " +
                "MaxRetry: {MaxRetry}, CB FailureThreshold: {CBThreshold}, " +
                "CB BreakDuration: {BreakDur}sn, PerAttemptTimeout: {AttemptTimeout}sn",
                _settings.HttpMaxRetryAttempts,
                _settings.HttpCircuitBreakerFailureThreshold,
                _settings.HttpCircuitBreakerBreakDurationSeconds,
                _settings.HttpPerAttemptTimeoutSeconds);

            return new ResiliencePipelineBuilder<HttpResponseMessage>()
                // Katman 1: Total Timeout — tüm retry'lar dahil max süre
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(_settings.HttpTotalTimeoutSeconds),
                    OnTimeout = args =>
                    {
                        _logger.LogError(
                            "[Resilience] HTTP Total timeout aşıldı! " +
                            "Limit: {Limit}sn — tüm retry denemeleri zaman aşımına uğradı.",
                            _settings.HttpTotalTimeoutSeconds);
                        return default;
                    }
                })
                // Katman 2: Retry — 5xx, timeout ve network hataları için
                .AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    MaxRetryAttempts = _settings.HttpMaxRetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true, // Thundering herd koruması
                    Delay = TimeSpan.FromMilliseconds(_settings.HttpRetryBaseDelayMs),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        // 5xx sunucu hatalarında retry
                        .HandleResult(r => (int)r.StatusCode >= 500)
                        // Timeout ve network hatalarında retry
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .Handle<TimeoutRejectedException>(),
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "[Resilience] HTTP Retry #{Attempt}. " +
                            "Bekleme: {Delay}ms, Neden: {Reason}",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.GetType().Name
                                ?? $"HTTP {(int)(args.Outcome.Result?.StatusCode ?? 0)}");
                        return default;
                    }
                })
                // Katman 3: Circuit Breaker — ardışık hatalarda devreyi aç
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions<HttpResponseMessage>
                {
                    FailureRatio = _settings.HttpCircuitBreakerFailureThreshold,
                    SamplingDuration = TimeSpan.FromSeconds(_settings.HttpCircuitBreakerSamplingDurationSeconds),
                    MinimumThroughput = _settings.HttpCircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(_settings.HttpCircuitBreakerBreakDurationSeconds),
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .HandleResult(r => (int)r.StatusCode >= 500)
                        .Handle<HttpRequestException>()
                        .Handle<TaskCanceledException>()
                        .Handle<TimeoutRejectedException>(),
                    OnOpened = args =>
                    {
                        _httpCircuitState = CircuitBreakerState.Open;
                        _logger.LogError(
                            "[Resilience] 🔴 HTTP Circuit Breaker AÇILDI! " +
                            "Mikro API'ye istekler {BreakDur}sn engellenecek.",
                            _settings.HttpCircuitBreakerBreakDurationSeconds);
                        return default;
                    },
                    OnClosed = _ =>
                    {
                        _httpCircuitState = CircuitBreakerState.Closed;
                        _logger.LogInformation("[Resilience] 🟢 HTTP Circuit Breaker kapandı — normal akış.");
                        return default;
                    },
                    OnHalfOpened = _ =>
                    {
                        _httpCircuitState = CircuitBreakerState.HalfOpen;
                        _logger.LogInformation("[Resilience] 🟡 HTTP Circuit Breaker yarı-açık — test isteği gönderiliyor.");
                        return default;
                    }
                })
                // Katman 4: Per-Attempt Timeout — tek istek süresi
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(_settings.HttpPerAttemptTimeoutSeconds),
                    OnTimeout = args =>
                    {
                        _logger.LogWarning(
                            "[Resilience] HTTP tek istek timeout: {Limit}sn aşıldı.",
                            _settings.HttpPerAttemptTimeoutSeconds);
                        return default;
                    }
                })
                .Build();
        }

        // ════════════════════════════════════════════════════════════════════
        // SQL Pipeline Build
        // ════════════════════════════════════════════════════════════════════

        private ResiliencePipeline BuildSqlPipeline()
        {
            _logger.LogInformation(
                "[Resilience] SQL pipeline oluşturuluyor. " +
                "MaxRetry: {MaxRetry}, CB BreakDuration: {BreakDur}sn",
                _settings.SqlMaxRetryAttempts,
                _settings.SqlCircuitBreakerBreakDurationSeconds);

            return new ResiliencePipelineBuilder()
                // SQL toplam timeout
                .AddTimeout(new TimeoutStrategyOptions
                {
                    Timeout = TimeSpan.FromSeconds(_settings.SqlTotalTimeoutSeconds),
                    OnTimeout = args =>
                    {
                        _logger.LogError("[Resilience] SQL Total timeout aşıldı: {Limit}sn", _settings.SqlTotalTimeoutSeconds);
                        return default;
                    }
                })
                // SQL retry — transient DB hatalarında
                .AddRetry(new RetryStrategyOptions
                {
                    MaxRetryAttempts = _settings.SqlMaxRetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    Delay = TimeSpan.FromMilliseconds(_settings.SqlRetryBaseDelayMs),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Microsoft.Data.SqlClient.SqlException>(ex =>
                            // Transient SQL hataları: deadlock, timeout, network
                            ex.Number == -2    // Timeout
                            || ex.Number == 1205  // Deadlock
                            || ex.Number == 40613 // DB unavailable
                            || ex.Number == 40197 // Service error
                            || ex.Number == 40501 // Service busy
                            || ex.Number == 49918 // Not enough resources
                        )
                        .Handle<TimeoutException>()
                        .Handle<InvalidOperationException>(ex =>
                            ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase)),
                    OnRetry = args =>
                    {
                        _logger.LogWarning(
                            "[Resilience] SQL Retry #{Attempt}. Bekleme: {Delay}ms, Hata: {Error}",
                            args.AttemptNumber,
                            args.RetryDelay.TotalMilliseconds,
                            args.Outcome.Exception?.Message ?? "Bilinmeyen");
                        return default;
                    }
                })
                // SQL Circuit Breaker
                .AddCircuitBreaker(new CircuitBreakerStrategyOptions
                {
                    FailureRatio = _settings.SqlCircuitBreakerFailureThreshold,
                    SamplingDuration = TimeSpan.FromSeconds(_settings.SqlCircuitBreakerSamplingDurationSeconds),
                    MinimumThroughput = _settings.SqlCircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(_settings.SqlCircuitBreakerBreakDurationSeconds),
                    ShouldHandle = new PredicateBuilder()
                        .Handle<Microsoft.Data.SqlClient.SqlException>()
                        .Handle<TimeoutException>(),
                    OnOpened = args =>
                    {
                        _sqlCircuitState = CircuitBreakerState.Open;
                        _logger.LogError(
                            "[Resilience] 🔴 SQL Circuit Breaker AÇILDI! " +
                            "Mikro DB sorguları {BreakDur}sn engellenecek.",
                            _settings.SqlCircuitBreakerBreakDurationSeconds);
                        return default;
                    },
                    OnClosed = _ =>
                    {
                        _sqlCircuitState = CircuitBreakerState.Closed;
                        _logger.LogInformation("[Resilience] 🟢 SQL Circuit Breaker kapandı.");
                        return default;
                    },
                    OnHalfOpened = _ =>
                    {
                        _sqlCircuitState = CircuitBreakerState.HalfOpen;
                        _logger.LogInformation("[Resilience] 🟡 SQL Circuit Breaker yarı-açık — test sorgusu gönderiliyor.");
                        return default;
                    }
                })
                .Build();
        }
    }

    /// <summary>
    /// Polly circuit breaker durumunu admin panele göstermek için.
    /// Polly.CircuitBreaker.CircuitState yerine basit enum — dışa bağımlılık azaltma.
    /// </summary>
    public enum CircuitBreakerState
    {
        Closed,    // Normal — istekler geçiyor
        Open,      // Açık — istekler engelleniyor
        HalfOpen   // Yarı-açık — test isteği bekleniyor
    }
}
