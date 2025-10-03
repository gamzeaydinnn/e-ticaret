//	• ProductVariants (Id, ProductId, SKU, Price, Cost, VAT, Attributes(JSON), Weight, Barcode, RowVersion)
using ECommerce.Entities.Concrete;
namespace ECommerce.Entities.Concrete
{
    public class ProductVariant : BaseEntity
{
    public int ProductId { get; set; }
    public string Title { get; set; }         // e.g. "Kırmızı / M"
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string SKU { get; set; }
    public Product Product { get; set; }
}
}