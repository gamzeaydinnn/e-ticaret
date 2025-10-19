using System;

namespace ECommerce.Entities.Concrete
{
    public class MicroSyncLog
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
        public string? InternalId { get; set; }
        public string Direction { get; set; } = "ToERP";
        public string Status { get; set; } = "Pending";
        public int Attempts { get; set; } = 0;
        public string? LastError { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Kısa özet metin için Message alanı
        public string Message { get; set; } = string.Empty;
    }
}
