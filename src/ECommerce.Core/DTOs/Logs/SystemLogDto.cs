using System;

namespace ECommerce.Core.DTOs.Logs
{
    public class SystemLogDto
    {
        public int Id { get; set; }
        public string EntityType { get; set; } = string.Empty;
        public string? ExternalId { get; set; }
        public string? InternalId { get; set; }
        public string Direction { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Attempts { get; set; }
        public string? LastError { get; set; }
        public DateTime? LastAttemptAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
