using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using ECommerce.Entities.Enums;

namespace ECommerce.Core.DTOs.Courier
{
    /// <summary>
    /// Kuryeye atanan sipariş listesi için DTO.
    /// Mobil uyumlu, özet bilgi içerir.
    /// </summary>
    public class CourierOrderListDto
    {
        /// <summary>
        /// Sipariş ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Sipariş numarası (görüntüleme için)
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Müşteri adı (kısaltılmış olabilir - KVKK)
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Teslimat adresi (kısaltılmış, maks 50 karakter)
        /// </summary>
        public string AddressSummary { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş toplam tutarı
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Para birimi
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Sipariş durumu
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Durum badge rengi (frontend için)
        /// </summary>
        public string StatusColor { get; set; } = string.Empty;

        /// <summary>
        /// Durum Türkçe açıklama
        /// </summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>
        /// Ödeme yöntemi
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Ödeme durumu
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş önceliği (normal, urgent, low)
        /// </summary>
        public string Priority { get; set; } = "normal";

        /// <summary>
        /// Kuryeye atanma tarihi
        /// </summary>
        public DateTime? AssignedAt { get; set; }

        /// <summary>
        /// Sipariş tarihi
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Tahmini teslimat zamanı
        /// </summary>
        public DateTime? EstimatedDelivery { get; set; }

        /// <summary>
        /// Ürün sayısı
        /// </summary>
        public int ItemCount { get; set; }

        /// <summary>
        /// Müşteri notu var mı?
        /// </summary>
        public bool HasCustomerNote { get; set; }
    }

    /// <summary>
    /// Kurye için sipariş detay DTO.
    /// Tam bilgi içerir (adres, telefon, ürünler vs.)
    /// </summary>
    public class CourierOrderDetailDto
    {
        /// <summary>
        /// Sipariş ID
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Sipariş numarası
        /// </summary>
        public string OrderNumber { get; set; } = string.Empty;

        /// <summary>
        /// Sipariş durumu
        /// </summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>
        /// Durum Türkçe açıklama
        /// </summary>
        public string StatusText { get; set; } = string.Empty;

        #region Müşteri Bilgileri

        /// <summary>
        /// Müşteri tam adı
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Müşteri telefon numarası
        /// </summary>
        public string CustomerPhone { get; set; } = string.Empty;

        /// <summary>
        /// Müşteri e-posta (opsiyonel)
        /// </summary>
        public string? CustomerEmail { get; set; }

        #endregion

        #region Adres Bilgileri

        /// <summary>
        /// Tam teslimat adresi
        /// </summary>
        public string FullAddress { get; set; } = string.Empty;

        /// <summary>
        /// Şehir
        /// </summary>
        public string City { get; set; } = string.Empty;

        /// <summary>
        /// Adres koordinatları (Google Maps için)
        /// </summary>
        public string? Coordinates { get; set; }

        /// <summary>
        /// Google Maps URL
        /// </summary>
        public string? GoogleMapsUrl { get; set; }

        #endregion

        #region Ödeme Bilgileri

        /// <summary>
        /// Sipariş toplam tutarı
        /// </summary>
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Para birimi
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Ödeme yöntemi (Kredi Kartı, Kapıda Ödeme vs.)
        /// </summary>
        public string PaymentMethod { get; set; } = string.Empty;

        /// <summary>
        /// Ödeme durumu
        /// </summary>
        public string PaymentStatus { get; set; } = string.Empty;

        /// <summary>
        /// Ödeme bilgisi (Kurye için gösterilecek metin)
        /// Örnek: "Kredi Kartı (Provizyon alındı)" veya "Kapıda Nakit"
        /// </summary>
        public string PaymentInfo { get; set; } = string.Empty;

        /// <summary>
        /// Kapıda ödeme ise tahsil edilecek tutar
        /// </summary>
        public decimal? CashOnDeliveryAmount { get; set; }

        #endregion

        #region Sipariş Bilgileri

        /// <summary>
        /// Sipariş tarihi
        /// </summary>
        public DateTime OrderDate { get; set; }

        /// <summary>
        /// Kuryeye atanma tarihi
        /// </summary>
        public DateTime? AssignedAt { get; set; }

        /// <summary>
        /// Yola çıkış tarihi
        /// </summary>
        public DateTime? PickedUpAt { get; set; }

        /// <summary>
        /// Teslim tarihi
        /// </summary>
        public DateTime? DeliveredAt { get; set; }

        /// <summary>
        /// Tahmini teslimat zamanı
        /// </summary>
        public DateTime? EstimatedDelivery { get; set; }

        /// <summary>
        /// Sipariş önceliği
        /// </summary>
        public string Priority { get; set; } = "normal";

        #endregion

        #region Notlar

        /// <summary>
        /// Müşteri notu (varsa sarı arka planla gösterilmeli)
        /// </summary>
        public string? CustomerNote { get; set; }

        /// <summary>
        /// Teslimat notu (adres tarifi vs.)
        /// </summary>
        public string? DeliveryNote { get; set; }

        #endregion

        /// <summary>
        /// Sipariş kalemleri
        /// </summary>
        public List<CourierOrderItemDto> Items { get; set; } = new();

        /// <summary>
        /// Kurye için izin verilen aksiyonlar
        /// </summary>
        public CourierAllowedActions AllowedActions { get; set; } = new();
    }

    /// <summary>
    /// Sipariş kalemi DTO (kurye görünümü)
    /// </summary>
    public class CourierOrderItemDto
    {
        /// <summary>
        /// Ürün ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Ürün adı
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Adet
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Birim fiyat
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Toplam fiyat (Quantity * UnitPrice)
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Birim (adet, kg, gram vs.)
        /// </summary>
        public string Unit { get; set; } = "adet";

        /// <summary>
        /// Tartı farkı var mı? (Ağırlık bazlı ürünler için)
        /// </summary>
        public bool HasWeightDifference { get; set; }

        /// <summary>
        /// Beklenen ağırlık (gram)
        /// </summary>
        public decimal? ExpectedWeightGrams { get; set; }

        /// <summary>
        /// Gerçek ağırlık (gram)
        /// </summary>
        public decimal? ActualWeightGrams { get; set; }

        /// <summary>
        /// Ağırlık farkından kaynaklanan ek tutar
        /// </summary>
        public decimal? WeightDifferenceAmount { get; set; }
    }

    /// <summary>
    /// Kurye için izin verilen aksiyonlar
    /// </summary>
    public class CourierAllowedActions
    {
        /// <summary>
        /// "Yola Çıktım" butonu aktif mi?
        /// ASSIGNED durumunda true
        /// </summary>
        public bool CanStartDelivery { get; set; }

        /// <summary>
        /// "Teslim Ettim" butonu aktif mi?
        /// OUT_FOR_DELIVERY durumunda true
        /// </summary>
        public bool CanMarkDelivered { get; set; }

        /// <summary>
        /// "Sorun Var" butonu aktif mi?
        /// Her zaman true (terminal durumlar hariç)
        /// </summary>
        public bool CanReportProblem { get; set; }

        /// <summary>
        /// Müşteriyi arayabilir mi?
        /// Telefon numarası varsa true
        /// </summary>
        public bool CanCallCustomer { get; set; }

        /// <summary>
        /// Haritada gösterebilir mi?
        /// Adres varsa true
        /// </summary>
        public bool CanShowOnMap { get; set; }
    }
}
