// ProductVariant: Ürünün satın alınabilir varyantlarını temsil eder.
// XML/SKU entegrasyonu için genişletilmiş versiyon.
// Her varyant benzersiz bir SKU'ya sahiptir ve sepet/sipariş işlemleri varyant bazlı çalışır.
// Örnek: Product="Coca-Cola", Variant="Coca-Cola 330ml", "Coca-Cola 1L", "Coca-Cola 6'lı paket"

using ECommerce.Entities.Concrete;
using System.Collections.Generic;
using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ürün varyantı entity'si.
    /// Altın Kural: OrderItem her zaman variant_id tutar çünkü sipariş anındaki fiyat/stok variant'a aittir.
    /// </summary>
    public class ProductVariant : BaseEntity
    {
        #region Temel Alanlar

        /// <summary>
        /// Ana ürün ID'si (Foreign Key)
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Varyant başlığı (örn: "330ml", "1L", "6'lı Paket")
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Varyant fiyatı - Sipariş anında bu fiyat kullanılır
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// Stok miktarı
        /// </summary>
        public int Stock { get; set; }

        /// <summary>
        /// Stok Tutma Birimi - XML entegrasyonunun ana anahtarı (UNIQUE)
        /// Tedarikçi sistemleriyle eşleşme bu alan üzerinden yapılır
        /// </summary>
        public string SKU { get; set; } = string.Empty;

        #endregion

        #region XML/Entegrasyon Alanları

        /// <summary>
        /// Barkod numarası (EAN/UPC)
        /// Tedarikçi XML'inden veya manuel girişten gelebilir
        /// </summary>
        public string? Barcode { get; set; }

        /// <summary>
        /// Para birimi kodu (ISO 4217)
        /// Varsayılan: TRY
        /// </summary>
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Ağırlık (gram cinsinden)
        /// Kargo hesaplaması ve tartı entegrasyonu için kullanılır
        /// </summary>
        public int? WeightGrams { get; set; }

        /// <summary>
        /// Hacim (mililitre cinsinden)
        /// İçecek ve sıvı ürünler için kullanılır
        /// </summary>
        public int? VolumeML { get; set; }

        /// <summary>
        /// Son XML/feed senkronizasyon zamanı
        /// Import işlemlerinde güncellenir
        /// </summary>
        public DateTime? LastSyncedAt { get; set; }

        /// <summary>
        /// Feed'de en son görüldüğü zaman
        /// X gün görünmeyen varyantlar pasifleştirilebilir
        /// </summary>
        public DateTime? LastSeenAt { get; set; }

        /// <summary>
        /// Tedarikçi/supplier kodu
        /// Hangi tedarikçiden geldiğini izlemek için
        /// </summary>
        public string? SupplierCode { get; set; }

        /// <summary>
        /// Ana ürün grubu SKU'su
        /// Tedarikçi XML'inde ParentSku/GroupCode varsa buraya yazılır
        /// Aynı parentSku'ya sahip varyantlar aynı product'a bağlanır
        /// </summary>
        public string? ParentSku { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Ana ürün (zorunlu ilişki)
        /// </summary>
        public Product Product { get; set; } = null!;

        /// <summary>
        /// Stok kayıtları
        /// </summary>
        public virtual ICollection<Stocks> Stocks { get; set; } = new HashSet<Stocks>();

        /// <summary>
        /// Varyanta atanmış seçenek değerleri (Hacim=330ml, Renk=Kırmızı vb.)
        /// Many-to-Many ilişki: VariantOptionValue üzerinden
        /// </summary>
        public virtual ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new HashSet<VariantOptionValue>();

        #endregion
    }
}