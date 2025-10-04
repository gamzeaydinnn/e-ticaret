//	• AuditLogs (Id, Entity, Action, UserId, Changes JSON, CreatedAt)
//	• ProductVariants (Id, ProductId, SKU, Price, Cost, VAT, Attributes(JSON), Weight, Barcode, RowVersion)
using ECommerce.Entities.Concrete;
namespace ECommerce.Entities.Concrete
{
    public class AuditLogs : BaseEntity
{
    public int UserId { get; set; }
        public string Action { get; set; } = string.Empty;      // default değer atandı
        public string EntityName { get; set; } = string.Empty;  // default değer atandı
        public int EntityId { get; set; }
}
}