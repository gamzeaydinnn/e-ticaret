using System;

namespace ECommerce.Core.Entities.Concrete
{
    public class StockMovement
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int QuantityChange { get; set; }
        public DateTime MovementDate { get; set; }
        public string Reason { get; set; } // Örn: "Sale", "Return", "Manual"
    }
}
/*Kullanım amacı:
Stok takibi: Hangi ürün ne zaman, ne kadar azaldı veya arttı, kaydını tutar.
Raporlama: Satış, iade, stok sayımı gibi durumlarda rapor çıkarmak için kullanılır.
Denetim: Ürün stoklarındaki değişikliklerin izlenebilir olmasını sağlar.
Özetle, StockMovement stok değişikliklerinin tarihçesini tutan bir kayıt sistemidir.*/