// CartItemDto: Sepet kalemi bilgilerini döndüren DTO.
// Varyant desteği eklendi - hangi varyantın sepete eklendiği izlenir.

using System;

namespace ECommerce.Core.DTOs.Cart
{
    /// <summary>
    /// Sepet kalemi bilgilerini içeren DTO.
    /// Sepet görüntüleme ve güncelleme işlemlerinde kullanılır.
    /// </summary>
    public class CartItemDto
    {
        #region Kimlik

        /// <summary>
        /// Sepet kalemi ID
        /// </summary>
        public int Id { get; set; }

        #endregion

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
        /// Ürün görseli URL
        /// </summary>
        public string? ProductImageUrl { get; set; }

        /// <summary>
        /// ProductImage alias (frontend uyumluluğu için)
        /// </summary>
        public string? ProductImage 
        { 
            get => ProductImageUrl; 
            set => ProductImageUrl = value; 
        }

        /// <summary>
        /// Sku alias (frontend uyumluluğu için)
        /// </summary>
        public string? Sku { get; set; }

        #endregion

        #region Varyant Bilgileri

        /// <summary>
        /// Varyant ID (nullable - varyantsız ürünler için null)
        /// </summary>
        public int? ProductVariantId { get; set; }

        /// <summary>
        /// VariantId alias (frontend uyumluluğu için)
        /// </summary>
        public int? VariantId 
        { 
            get => ProductVariantId; 
            set => ProductVariantId = value; 
        }

        /// <summary>
        /// Varyant başlığı
        /// Örn: "Coca Cola 330ml Kutu"
        /// </summary>
        public string? VariantTitle { get; set; }

        /// <summary>
        /// Varyant SKU'su
        /// </summary>
        public string? VariantSku { get; set; }

        /// <summary>
        /// Varyant seçenek özeti
        /// Örn: "Hacim: 330ml"
        /// </summary>
        public string? VariantOptions { get; set; }

        /// <summary>
        /// Varyant mı?
        /// </summary>
        public bool IsVariant => ProductVariantId.HasValue;

        #endregion

        #region Fiyat ve Adet

        /// <summary>
        /// Adet
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Birim fiyat
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// Toplam fiyat
        /// </summary>
        public decimal TotalPrice => Quantity * UnitPrice;

        /// <summary>
        /// Formatlanmış birim fiyat
        /// </summary>
        public string FormattedUnitPrice => $"₺{UnitPrice:N2}";

        /// <summary>
        /// Formatlanmış toplam fiyat
        /// </summary>
        public string FormattedTotalPrice => $"₺{TotalPrice:N2}";

        #endregion

        #region Stok Durumu

        /// <summary>
        /// Mevcut stok adedi (varyant veya ana ürün)
        /// Stok yetersizliği uyarısı için
        /// </summary>
        public int AvailableStock { get; set; }

        /// <summary>
        /// Stok yeterli mi?
        /// </summary>
        public bool IsStockSufficient => AvailableStock >= Quantity;

        /// <summary>
        /// Stok uyarı mesajı (yetersizse)
        /// </summary>
        public string? StockWarning => !IsStockSufficient 
            ? $"Stokta sadece {AvailableStock} adet kaldı" 
            : null;

        #endregion

        #region Görüntüleme

        /// <summary>
        /// Gösterilecek başlık
        /// </summary>
        public string DisplayTitle => !string.IsNullOrEmpty(VariantTitle) 
            ? VariantTitle 
            : ProductName;

        #endregion
    }

    /// <summary>
    /// Sepete ürün eklemek için kullanılan DTO.
    /// </summary>
    public class AddToCartDto
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
        /// Eklenecek adet
        /// </summary>
        public int Quantity { get; set; } = 1;
    }

    /// <summary>
    /// Sepet kalemi güncellemek için kullanılan DTO.
    /// </summary>
    public class UpdateCartItemDto
    {
        /// <summary>
        /// Yeni adet
        /// 0 gönderilirse kalem silinir
        /// </summary>
        public int Quantity { get; set; }
    }
}
