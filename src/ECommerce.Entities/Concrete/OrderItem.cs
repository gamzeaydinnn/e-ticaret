// OrderItem: Sipariş kalemi entity'si.
// XML/Varyant entegrasyonu sonrası güncellendi.
// ALTIN KURAL: OrderItem her zaman variant_id tutar çünkü sipariş anındaki fiyat/stok variant'a aittir.
// ProductVariantId nullable çünkü mevcut eski siparişlerde bu alan boş olacak (backward compatibility).
// AĞIRLIK BAZLI SİSTEM: Tartı sonucu fiyat değişikliği için yeni alanlar eklendi.

using ECommerce.Entities.Concrete;
using ECommerce.Entities.Enums;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Sipariş kalemi entity'si.
    /// Varyant bazlı sipariş sistemi için ProductVariantId eklendi.
    /// Ağırlık bazlı dinamik fiyatlandırma için tahmini/gerçek miktar alanları eklendi.
    /// </summary>
    public class OrderItem : BaseEntity
    {
        #region Temel Alanlar

        /// <summary>
        /// Sipariş ID'si (Foreign Key)
        /// </summary>
        public int OrderId { get; set; }

        /// <summary>
        /// Ürün ID'si (Foreign Key)
        /// Geriye dönük uyumluluk için korundu
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Sipariş edilen miktar (adet bazlı ürünler için)
        /// Ağırlık bazlı ürünlerde bu değer 1 olur, miktar EstimatedWeight'te tutulur
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Sipariş anındaki birim fiyat
        /// Fiyat değişse bile sipariş anındaki fiyat korunur
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Sipariş anında beklenen toplam ağırlık (gram)
        /// Hesaplama: Product.UnitWeightGrams * Quantity
        /// </summary>
        public int ExpectedWeightGrams { get; set; }

        #endregion

        #region Ağırlık Bazlı Satış Alanları

        /// <summary>
        /// Bu ürün ağırlık bazlı mı? (Product.IsWeightBased'den kopyalanır)
        /// Sipariş anında snapshot alınır, sonradan ürün değişse bile etkilenmez
        /// </summary>
        public bool IsWeightBased { get; set; } = false;

        /// <summary>
        /// Ağırlık birimi (kg, gram, litre vb.)
        /// Sipariş anında Product'tan kopyalanır
        /// </summary>
        public WeightUnit WeightUnit { get; set; } = WeightUnit.Piece;

        /// <summary>
        /// Tahmini ağırlık (gram cinsinden)
        /// Müşterinin sipariş ettiği miktar
        /// Örn: 1 kg domates = 1000 gram
        /// </summary>
        public decimal EstimatedWeight { get; set; } = 0m;

        /// <summary>
        /// Gerçek ağırlık (gram cinsinden)
        /// Kurye tarafından tartıldıktan sonra girilir
        /// Örn: 1.1 kg domates = 1100 gram
        /// </summary>
        public decimal? ActualWeight { get; set; }

        /// <summary>
        /// Tahmini fiyat (TL)
        /// Sipariş anında hesaplanan tutar, provizyon bu tutara göre alınır
        /// Hesaplama: EstimatedWeight * (PricePerUnit / birim)
        /// </summary>
        public decimal EstimatedPrice { get; set; } = 0m;

        /// <summary>
        /// Gerçek fiyat (TL)
        /// Tartı sonrası hesaplanan kesin tutar
        /// Hesaplama: ActualWeight * (PricePerUnit / birim)
        /// </summary>
        public decimal? ActualPrice { get; set; }

        /// <summary>
        /// Birim fiyat (sipariş anında snapshot)
        /// Ürün fiyatı değişse bile sipariş anındaki fiyat korunur
        /// </summary>
        public decimal PricePerUnit { get; set; } = 0m;

        /// <summary>
        /// Ağırlık farkı (gram cinsinden)
        /// Pozitif: Fazla geldi, Negatif: Eksik geldi
        /// Hesaplama: ActualWeight - EstimatedWeight
        /// </summary>
        public decimal? WeightDifference { get; set; }

        /// <summary>
        /// Fiyat farkı (TL)
        /// Pozitif: Ek ödeme gerekli, Negatif: İade gerekli
        /// Hesaplama: ActualPrice - EstimatedPrice
        /// </summary>
        public decimal? PriceDifference { get; set; }

        /// <summary>
        /// Tartı yapıldı mı?
        /// false: Henüz tartılmadı
        /// true: Kurye tarafından tartıldı
        /// </summary>
        public bool IsWeighed { get; set; } = false;

        /// <summary>
        /// Tartı tarihi
        /// Kurye tarafından tartıldığı an
        /// </summary>
        public DateTime? WeighedAt { get; set; }

        /// <summary>
        /// Tartıyı yapan kurye ID'si
        /// </summary>
        public int? WeighedByCourierId { get; set; }

        #endregion

        #region Varyant Alanları (XML/SKU Entegrasyonu)

        /// <summary>
        /// Varyant ID'si (Foreign Key, nullable)
        /// Yeni siparişlerde her zaman dolu olmalı.
        /// Null: Eski siparişler veya varyant yoksa
        /// </summary>
        public int? ProductVariantId { get; set; }

        /// <summary>
        /// Sipariş anındaki varyant başlığı (snapshot)
        /// Varyant silinse/değişse bile sipariş kaydında orijinal değer korunur
        /// Örn: "Coca-Cola 330ml"
        /// </summary>
        public string? VariantTitle { get; set; }

        /// <summary>
        /// Sipariş anındaki SKU (snapshot)
        /// Raporlama ve takip için kullanılır
        /// </summary>
        public string? VariantSku { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Sipariş (parent)
        /// </summary>
        public virtual Order? Order { get; set; }

        /// <summary>
        /// Ürün
        /// </summary>
        public virtual Product? Product { get; set; }

        /// <summary>
        /// Varyant (nullable - eski siparişler için)
        /// </summary>
        public virtual ProductVariant? ProductVariant { get; set; }

        /// <summary>
        /// Ağırlık raporları (tartı entegrasyonu)
        /// </summary>
        public virtual ICollection<WeightReport> WeightReports { get; set; } = new HashSet<WeightReport>();

        #endregion
    }
}
