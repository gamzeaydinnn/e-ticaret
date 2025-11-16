using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Entities.Enums
{
    public enum OrderStatus
    {
        Pending,        // Beklemede
        Preparing,      // Hazırlanıyor  
        Ready,          // Teslim alınmaya hazır
        Assigned,       // Kuryeye atandı
        PickedUp,       // Kurye tarafından teslim alındı
        InTransit,      // Yolda
        Delivered,      // Teslim edildi
        Cancelled,      // İptal edildi
        Processing,     // İşleniyor (eski)
        Shipped,        // Kargoya verildi (eski)
        Paid,           // Ödendi
        Completed,      // Tamamlandı
        PaymentFailed   // Ödeme başarısız
    }
}
