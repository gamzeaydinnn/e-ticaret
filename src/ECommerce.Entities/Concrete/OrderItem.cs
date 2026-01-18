// OrderItem: Sipariş kalemi entity'si.
// XML/Varyant entegrasyonu sonrası güncellendi.
// ALTIN KURAL: OrderItem her zaman variant_id tutar çünkü sipariş anındaki fiyat/stok variant'a aittir.
// ProductVariantId nullable çünkü mevcut eski siparişlerde bu alan boş olacak (backward compatibility).

using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Sipariş kalemi entity'si.
    /// Varyant bazlı sipariş sistemi için ProductVariantId eklendi.
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
        /// Sipariş edilen miktar
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
