using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Sync operasyonlarının detaylı denetim kaydı.
    /// 
    /// NEDEN: MicroSyncLog retry/push odaklı, ancak çakışma kararları, circuit breaker
    /// durum değişiklikleri ve alert geçmişi kayboluyordu. Bu tablo tüm kritik sync
    /// olaylarını kalıcı olarak saklar — audit trail ve troubleshooting için.
    /// 
    /// OLAY TİPLERİ:
    /// - Conflict: Stok/fiyat çakışması ve çözüm kararı
    /// - CircuitBreaker: CB açılma/kapanma olayları
    /// - Alert: Eşik aşımı uyarıları (ardışık hata, düşük başarı oranı)
    /// - DeadLetter: İşlem kalıcı başarısızlığa taşındı
    /// - Retry: Retry deneme sonucu (başarı/başarısızlık)
    /// - ManualAction: Admin tarafından yapılan manuel müdahale
    /// </summary>
    public class SyncAuditLog
    {
        public int Id { get; set; }

        /// <summary>Olay tipi: Conflict, CircuitBreaker, Alert, DeadLetter, Retry, ManualAction</summary>
        public string EventType { get; set; } = string.Empty;

        /// <summary>Olay önem derecesi: Info, Warning, Error, Critical</summary>
        public string Severity { get; set; } = "Info";

        /// <summary>Kaynak servis/kanal: HotPoll, OutboundSync, RetryService, CircuitBreaker vb.</summary>
        public string Source { get; set; } = string.Empty;

        /// <summary>İlgili stok kodu veya entity ID (varsa)</summary>
        public string? EntityId { get; set; }

        /// <summary>Olay açıklaması — insan okunabilir</summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>Ek detay JSON: çakışma değerleri, CB state, alert metadata</summary>
        public string? Details { get; set; }

        /// <summary>Correlation ID — aynı operasyondaki olayları birleştirmek için</summary>
        public string? CorrelationId { get; set; }

        /// <summary>Olayın oluşturulma zamanı (UTC)</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
