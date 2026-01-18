// ProductVariantCreateDto: Yeni varyant oluşturma için kullanılan DTO.
// Validation kuralları ile güvenli veri girişi sağlanır.
// SKU benzersizliği business katmanında kontrol edilecektir.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Product
{
    /// <summary>
    /// Yeni ürün varyantı oluşturmak için kullanılan DTO.
    /// Tüm zorunlu alanlar validation ile korunur.
    /// </summary>
    public class ProductVariantCreateDto
    {
        #region Zorunlu Alanlar

        /// <summary>
        /// Varyant başlığı - Müşteriye gösterilecek isim
        /// Örn: "Coca Cola 330ml", "iPhone 15 Pro 256GB Siyah"
        /// </summary>
        [Required(ErrorMessage = "Varyant başlığı zorunludur")]
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Başlık 2-200 karakter arasında olmalıdır")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Stok Tutma Birimi - Benzersiz ürün kodu (UNIQUE)
        /// XML entegrasyonunun ana anahtarı
        /// Örn: "CC-330ML", "IP15P-256-BLK"
        /// </summary>
        [Required(ErrorMessage = "SKU zorunludur")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "SKU 2-50 karakter arasında olmalıdır")]
        [RegularExpression(@"^[A-Za-z0-9\-_]+$", ErrorMessage = "SKU sadece harf, rakam, tire ve alt çizgi içerebilir")]
        public string SKU { get; set; } = string.Empty;

        /// <summary>
        /// Varyant satış fiyatı
        /// </summary>
        [Required(ErrorMessage = "Fiyat zorunludur")]
        [Range(0.01, 9999999.99, ErrorMessage = "Fiyat 0.01 ile 9,999,999.99 arasında olmalıdır")]
        public decimal Price { get; set; }

        #endregion

        #region Stok Bilgileri

        /// <summary>
        /// Mevcut stok adedi
        /// Negatif stok kabul edilmez
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Stok negatif olamaz")]
        public int Stock { get; set; } = 0;

        #endregion

        #region Opsiyonel Alanlar

        /// <summary>
        /// Para birimi kodu (ISO 4217)
        /// Varsayılan: TRY
        /// </summary>
        [StringLength(10, ErrorMessage = "Para birimi en fazla 10 karakter olabilir")]
        public string Currency { get; set; } = "TRY";

        /// <summary>
        /// Barkod numarası (EAN, UPC, ISBN vb.)
        /// Opsiyonel - Tedarikçi XML'den gelebilir
        /// </summary>
        [StringLength(50, ErrorMessage = "Barkod en fazla 50 karakter olabilir")]
        public string? Barcode { get; set; }

        /// <summary>
        /// Ağırlık (gram cinsinden)
        /// Kargo hesaplaması için kullanılır
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "Ağırlık 0-1,000,000 gram arasında olmalıdır")]
        public int? WeightGrams { get; set; }

        /// <summary>
        /// Hacim (mililitre cinsinden)
        /// İçecek ürünleri için varyant ayrımında kullanılır
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "Hacim 0-1,000,000 ml arasında olmalıdır")]
        public int? VolumeML { get; set; }

        /// <summary>
        /// Tedarikçi ürün kodu
        /// XML entegrasyonunda tedarikçi referansı
        /// </summary>
        [StringLength(100, ErrorMessage = "Tedarikçi kodu en fazla 100 karakter olabilir")]
        public string? SupplierCode { get; set; }

        /// <summary>
        /// Ana ürün grubu SKU'su
        /// Aynı ürünün farklı varyantlarını gruplamak için
        /// Örn: Tüm "Coca Cola" ürünleri "CC-GROUP" ParentSku'ya sahip
        /// </summary>
        [StringLength(50, ErrorMessage = "ParentSku en fazla 50 karakter olabilir")]
        public string? ParentSku { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Pasif varyantlar satışta gösterilmez
        /// </summary>
        public bool IsActive { get; set; } = true;

        #endregion

        #region Option Değerleri

        /// <summary>
        /// Varyanta atanacak seçenek değer ID'leri
        /// Örn: Hacim=330ml (ID:1), Paket=Tekli (ID:5)
        /// Business katmanında geçerlilik kontrol edilir
        /// </summary>
        public List<int>? OptionValueIds { get; set; }

        #endregion
    }
}
