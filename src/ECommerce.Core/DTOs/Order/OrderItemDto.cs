// OrderItemDto: Sipariş kalemi bilgilerini döndüren DTO.
// Varyant desteği eklendi - sipariş anındaki varyant bilgisi snapshot olarak saklanır.

using System;

namespace ECommerce.Core.DTOs.Order
{
    /// <summary>
    /// Sipariş kalemi bilgilerini içeren DTO.
    /// Sipariş detaylarında ve sipariş oluşturmada kullanılır.
    /// </summary>
    public class OrderItemDto
    {
        #region Ürün Bilgileri

        /// <summary>
        /// Ürün ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Ürün adı
        /// </summary>
        public string ProductName { get; set; } = string.Empty;

        /// <summary>
        /// Ürün görseli URL (opsiyonel)
        /// </summary>
        public string? ProductImageUrl { get; set; }

        #endregion

        #region Varyant Bilgileri

        /// <summary>
        /// Varyant ID (nullable - varyantsız ürünler için null)
        /// Sipariş oluşturmada bu ID kullanılır
        /// </summary>
        public int? ProductVariantId { get; set; }

        /// <summary>
        /// Varyant başlığı (sipariş anındaki snapshot)
        /// Sonradan değişse bile siparişteki değer korunur
        /// Örn: "Coca Cola 330ml Kutu"
        /// </summary>
        public string? VariantTitle { get; set; }

        /// <summary>
        /// Varyant SKU'su (sipariş anındaki snapshot)
        /// Stok takibi ve referans için
        /// </summary>
        public string? VariantSku { get; set; }

        /// <summary>
        /// Varyant seçenek özeti
        /// Örn: "Hacim: 330ml, Paket: Tekli"
        /// </summary>
        public string? VariantOptions { get; set; }

        /// <summary>
        /// Varyant mı yoksa ana ürün mü?
        /// </summary>
        public bool IsVariant => ProductVariantId.HasValue;

        #endregion

        #region Sipariş Detayları

        /// <summary>
        /// Adet
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Birim fiyat (sipariş anındaki fiyat)
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Toplam fiyat (Quantity * UnitPrice)
        /// </summary>
        public decimal TotalPrice => Quantity * UnitPrice;

        #endregion

        #region Görüntüleme

        /// <summary>
        /// Gösterilecek başlık
        /// Varyant varsa varyant başlığı, yoksa ürün adı
        /// </summary>
        public string DisplayTitle => !string.IsNullOrEmpty(VariantTitle) 
            ? VariantTitle 
            : ProductName;

        /// <summary>
        /// Detay satırı
        /// Örn: "2 x ₺15,99 = ₺31,98"
        /// </summary>
        public string DisplayDetail => $"{Quantity} x ₺{UnitPrice:N2} = ₺{TotalPrice:N2}";

        #endregion
    }

    /// <summary>
    /// Sipariş oluşturmada kullanılan sipariş kalemi DTO'su.
    /// Client'tan gelen minimum veri seti.
    /// </summary>
    public class OrderItemCreateDto
    {
        /// <summary>
        /// Ürün ID
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Varyant ID (varyantsız ürünler için null)
        /// </summary>
        public int? ProductVariantId { get; set; }

        /// <summary>
        /// Adet
        /// </summary>
        public int Quantity { get; set; }
    }
}
