// CartItem: Sepet kalemi entity'si.
// XML/Varyant entegrasyonu sonrası güncellendi.
// Sepete ekleme işlemi artık varyant bazlı çalışır.
// ProductVariantId nullable çünkü mevcut sepet verilerinde bu alan boş olabilir (backward compatibility).

using System.Collections.Generic;
using ECommerce.Entities.Concrete;
using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Sepet kalemi entity'si.
    /// Varyant bazlı sepet sistemi için ProductVariantId eklendi.
    /// </summary>
    public class CartItem : BaseEntity
    {
        #region Temel Alanlar

        /// <summary>
        /// Kullanıcı ID'si (Foreign Key)
        /// Giriş yapmış kullanıcılar için
        /// Misafir kullanıcılar için NULL olabilir
        /// </summary>
        public int? UserId { get; set; }

        /// <summary>
        /// Ürün ID'si (Foreign Key)
        /// Geriye dönük uyumluluk için korundu
        /// </summary>
        public int ProductId { get; set; }

        /// <summary>
        /// Sepetteki miktar
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Misafir kullanıcı token'ı
        /// Giriş yapmamış kullanıcıların sepetini takip etmek için
        /// </summary>
        public string? CartToken { get; set; }

        #endregion

        #region Varyant Alanları (XML/SKU Entegrasyonu)

        /// <summary>
        /// Varyant ID'si (Foreign Key, nullable)
        /// Yeni sepet eklemelerinde her zaman dolu olmalı.
        /// Null: Eski veriler veya varyant yoksa
        /// </summary>
        public int? ProductVariantId { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Kullanıcı
        /// </summary>
        public virtual User? User { get; set; }

        /// <summary>
        /// Ürün
        /// </summary>
        public virtual Product? Product { get; set; }

        /// <summary>
        /// Varyant (nullable - eski veriler için)
        /// </summary>
        public virtual ProductVariant? ProductVariant { get; set; }

        #endregion
    }
}
