using System;

namespace ECommerce.Core.DTOs.Logs
{
    public class AuditLogDto
    {
        public int Id { get; set; }
        public int? AdminUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public string? PerformedBy { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
