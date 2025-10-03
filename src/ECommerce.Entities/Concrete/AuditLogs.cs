//	• AuditLogs (Id, Entity, Action, UserId, Changes JSON, CreatedAt)
//	• ProductVariants (Id, ProductId, SKU, Price, Cost, VAT, Attributes(JSON), Weight, Barcode, RowVersion)
using ECommerce.Entities.Concrete;
namespace ECommerce.Entities.Concrete
{
    public class AuditLog : BaseEntity
{
    public int UserId { get; set; }
    public string Action { get; set; } // "CreateProduct", "UpdateProduct", ...
    public string EntityName { get; set; }
    public int EntityId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
}