//	• AuditLogs (Id, Entity, Action, UserId, Changes JSON, CreatedAt)
//	• ProductVariants (Id, ProductId, SKU, Price, Cost, VAT, Attributes(JSON), Weight, Barcode, RowVersion)
using System;

namespace ECommerce.Entities.Concrete
{
    public class AuditLogs : BaseEntity
    {
        public int? UserId { get; set; }
        public string Action { get; set; } = string.Empty;      // default değer atandı
        public string EntityName { get; set; } = string.Empty;  // default değer atandı
        public int? EntityId { get; set; }

        // Değişiklikten önceki ve sonraki değerleri JSON olarak saklayalım
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }

        // Kim yaptı? (UserName / Email gibi serbest alan)
        public string? PerformedBy { get; set; }

        // BaseEntity.CreatedAt alanı zaten mevcut ancak okunabilirlik için alias niteliğinde bir property ekleyebiliriz.
        public DateTime PerformedAt => CreatedAt;
    }
}
