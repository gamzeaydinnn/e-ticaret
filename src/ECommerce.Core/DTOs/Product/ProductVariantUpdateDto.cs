// ProductVariantUpdateDto: Mevcut varyantı güncellemek için kullanılan DTO.
// Null alanlar güncellenmez, sadece dolu alanlar değiştirilir (Partial Update).
// SKU değişikliğine izin verilmez - benzersizlik garantisi için.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ECommerce.Core.DTOs.Product
{
    /// <summary>
    /// Mevcut ürün varyantını güncellemek için kullanılan DTO.
    /// Partial update desteklenir: null değerler güncellenmez.
    /// NOT: SKU değiştirilemez (immutable field).
    /// </summary>
    public class ProductVariantUpdateDto
    {
        #region Güncellenebilir Alanlar

        /// <summary>
        /// Varyant başlığı
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Başlık 2-200 karakter arasında olmalıdır")]
        public string? Title { get; set; }

        /// <summary>
        /// Satış fiyatı
        /// Null gönderilirse güncellenmez
        /// </summary>
        [Range(0.01, 9999999.99, ErrorMessage = "Fiyat 0.01 ile 9,999,999.99 arasında olmalıdır")]
        public decimal? Price { get; set; }

        /// <summary>
        /// Stok adedi
        /// Null gönderilirse güncellenmez
        /// Stok sıfırlanacaksa 0 gönderilmeli
        /// </summary>
        [Range(0, int.MaxValue, ErrorMessage = "Stok negatif olamaz")]
        public int? Stock { get; set; }

        /// <summary>
        /// Para birimi kodu
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(10, ErrorMessage = "Para birimi en fazla 10 karakter olabilir")]
        public string? Currency { get; set; }

        /// <summary>
        /// Barkod numarası
        /// Null: güncellenmez, Empty string: silinir
        /// </summary>
        [StringLength(50, ErrorMessage = "Barkod en fazla 50 karakter olabilir")]
        public string? Barcode { get; set; }

        /// <summary>
        /// Ağırlık (gram)
        /// Null gönderilirse güncellenmez
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "Ağırlık 0-1,000,000 gram arasında olmalıdır")]
        public int? WeightGrams { get; set; }

        /// <summary>
        /// Hacim (ml)
        /// Null gönderilirse güncellenmez
        /// </summary>
        [Range(0, 1000000, ErrorMessage = "Hacim 0-1,000,000 ml arasında olmalıdır")]
        public int? VolumeML { get; set; }

        /// <summary>
        /// Tedarikçi ürün kodu
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(100, ErrorMessage = "Tedarikçi kodu en fazla 100 karakter olabilir")]
        public string? SupplierCode { get; set; }

        /// <summary>
        /// Ana ürün grubu SKU'su
        /// Null gönderilirse güncellenmez
        /// </summary>
        [StringLength(50, ErrorMessage = "ParentSku en fazla 50 karakter olabilir")]
        public string? ParentSku { get; set; }

        /// <summary>
        /// Aktif/Pasif durumu
        /// Null gönderilirse güncellenmez
        /// </summary>
        public bool? IsActive { get; set; }

        #endregion

        #region Option Değerleri

        /// <summary>
        /// Güncellenecek seçenek değer ID'leri
        /// Null: Option'lar güncellenmez
        /// Boş liste: Tüm option'lar kaldırılır
        /// Dolu liste: Mevcut option'lar bu liste ile değiştirilir
        /// </summary>
        public List<int>? OptionValueIds { get; set; }

        #endregion

        #region Yardımcı Metodlar

        /// <summary>
        /// DTO'nun herhangi bir güncelleme içerip içermediğini kontrol eder.
        /// Tüm alanlar null ise güncelleme yapılmasına gerek yoktur.
        /// </summary>
        public bool HasAnyUpdate()
        {
            return Title != null ||
                   Price.HasValue ||
                   Stock.HasValue ||
                   Currency != null ||
                   Barcode != null ||
                   WeightGrams.HasValue ||
                   VolumeML.HasValue ||
                   SupplierCode != null ||
                   ParentSku != null ||
                   IsActive.HasValue ||
                   OptionValueIds != null;
        }

        #endregion
    }
}
