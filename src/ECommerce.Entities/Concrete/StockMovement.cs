using System;
using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;

//	• StockMovements (Id, ProductVariantId, ChangeQuantity, MovementType, ReferenceId, CreatedAt, Note)

namespace ECommerce.Entities.Concrete
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int QuantityChange { get; set; }
        public DateTime MovementDate { get; set; }
        public MovementType MovementType { get; set; } = MovementType.Sale;
        public string Reason { get; set; } = string.Empty;  // İsteğe bağlı serbest metin
    }
}
/*Kullanım amacı:
Stok takibi: Hangi ürün ne zaman, ne kadar azaldı veya arttı, kaydını tutar.
Raporlama: Satış, iade, stok sayımı gibi durumlarda rapor çıkarmak için kullanılır.
Denetim: Ürün stoklarındaki değişikliklerin izlenebilir olmasını sağlar.
Özetle, StockMovement stok değişikliklerinin tarihçesini tutan bir kayıt sistemidir.*/
