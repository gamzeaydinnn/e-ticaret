//	• MikroSyncLog (Id, EntityType, ExternalId, InternalId, Direction, Status, Attempts, LastError, LastAttemptAt)
using System;

namespace ECommerce.Entities.Concrete
{
    public class MicroSyncLog
    {
        public int Id { get; set; }
        public DateTime SyncDate { get; set; } = DateTime.UtcNow;
        public string EntityType { get; set; } = string.Empty; // Product, Stock, Order
        public string Status { get; set; } = string.Empty; // Success, Failed
        public string Message { get; set; } = string.Empty; // Hata veya detay mesajı
    }
}
