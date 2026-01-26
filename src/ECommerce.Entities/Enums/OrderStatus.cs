using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Entities.Enums
{
    /// <summary>
    /// Sipariş durumu enum'ı
    /// Sipariş yaşam döngüsündeki tüm durumları kapsar
    /// İzin verilen geçişler: New→Confirmed→Preparing→Assigned→OutForDelivery→Delivered
    /// 
    /// DURUM AKIŞI:
    /// ┌─────────────────────────────────────────────────────────────────────────────┐
    /// │  NEW (-1) → CONFIRMED (-2) → PREPARING → READY → ASSIGNED                   │
    /// │                                                      ↓                       │
    /// │                                              OUT_FOR_DELIVERY                │
    /// │                                                ↓           ↓                 │
    /// │                                           DELIVERED   DELIVERY_FAILED (-3)  │
    /// │                                                ↓           ↓                 │
    /// │                               DELIVERY_PAYMENT_PENDING (-4)  → ASSIGNED     │
    /// │                                                              (yeniden atama)│
    /// │                                                                              │
    /// │  Herhangi bir durum → CANCELLED (admin yetkisiyle)                           │
    /// └─────────────────────────────────────────────────────────────────────────────┘
    /// </summary>
    public enum OrderStatus
    {
        // ═══════════════════════════════════════════════════════════════════════════════
        // YENİ SİPARİŞ AKIŞI DURUMLARI (Negatif değerler - geriye uyumluluk için)
        // ═══════════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Sipariş yeni oluştu ve ödeme authorize edildi
        /// Provizyon alındı, admin onayı bekleniyor
        /// </summary>
        New = -1,
        
        /// <summary>
        /// Admin siparişi gördü ve onayladı
        /// Hazırlık sürecine geçilecek
        /// </summary>
        Confirmed = -2,
        
        /// <summary>
        /// Teslimat başarısız oldu
        /// Kurye problem bildirdi (adres bulunamadı, müşteri yok vs.)
        /// Admin yeniden kurye atayabilir veya iptal edebilir
        /// </summary>
        DeliveryFailed = -3,
        
        /// <summary>
        /// Teslim edildi ama ödeme çekilemedi
        /// final_amount > authorized_amount durumunda oluşur
        /// Admin müdahalesi gerektirir:
        /// - Farkı sil (işletme karşılar)
        /// - Müşteriyle iletişim (ek ödeme)
        /// - Siparişi iptal et
        /// </summary>
        DeliveryPaymentPending = -4,
        
        // ═══════════════════════════════════════════════════════════════════════════════
        // MEVCUT DURUMLAR (Geriye uyumluluk için korunuyor)
        // ═══════════════════════════════════════════════════════════════════════════════
        
        Pending = 0,        // Beklemede (eski, geriye uyumluluk için)
        Preparing,          // Hazırlanıyor  
        Ready,              // Teslim alınmaya hazır
        Assigned,           // Kuryeye atandı
        PickedUp,           // Kurye tarafından teslim alındı
        InTransit,          // Yolda
        OutForDelivery,     // Dağıtımda
        Delivered,          // Teslim edildi
        Cancelled,          // İptal edildi
        Processing,         // İşleniyor (eski)
        Shipped,            // Kargoya verildi (eski)
        Paid,               // Ödendi
        Completed,          // Tamamlandı
        PaymentFailed,      // Ödeme başarısız
        ChargebackPending,  // Chargeback / itiraz bekleniyor
        Refunded,           // İade edildi
        
        // ═══════════════════════════════════════════════════════════════════════════════
        // YENİ EKLENMİŞ DURUMLAR (v2.0)
        // ═══════════════════════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Sipariş hazır, kurye alımı bekliyor
        /// Depo/personel hazırlamayı tamamladı
        /// </summary>
        ReadyForPickup,
        
        /// <summary>
        /// Kısmi iade yapıldı
        /// Bazı ürünler iade edildi, sipariş hala aktif
        /// </summary>
        PartialRefund
    }
}
