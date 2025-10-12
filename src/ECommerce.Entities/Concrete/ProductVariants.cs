//	• ProductVariants (Id, ProductId, SKU, Price, Cost, VAT, Attributes(JSON), Weight, Barcode, RowVersion)
using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    public class ProductVariant : BaseEntity
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;   // default değer atandı
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public string SKU { get; set; } = string.Empty;     // default değer atandı
        public Product Product { get; set; } = null!;
        public virtual ICollection<Stocks> Stocks { get; set; } = new HashSet<Stocks>();

}
}