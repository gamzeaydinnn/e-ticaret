// ProductOptionValue: Seçenek türlerine ait değerleri tanımlar.
// Örnek: "Hacim" seçeneği için değerler: "330ml", "500ml", "1L", "2L"
// Her değer bir seçenek türüne (ProductOption) bağlıdır.
// UNIQUE constraint: Aynı seçenek içinde aynı değer tekrarlanamaz.

using System;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Seçenek değeri entity'si.
    /// Bir seçenek türünün alabileceği spesifik değerler.
    /// Örn: Hacim seçeneği için "330ml", "1L" vb.
    /// </summary>
    public class ProductOptionValue : BaseEntity
    {
        #region Temel Alanlar

        /// <summary>
        /// Bağlı olduğu seçenek türü ID'si (Foreign Key)
        /// </summary>
        public int OptionId { get; set; }

        /// <summary>
        /// Değer metni
        /// Örn: "330ml", "Kırmızı", "M", "6'lı Paket"
        /// UNIQUE: Aynı OptionId içinde aynı Value olamaz
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// Görüntüleme sırası
        /// Değerlerin UI'da hangi sırada gösterileceğini belirler
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Renk kodu (hex formatında, opsiyonel)
        /// Renk seçenekleri için kullanılır: "#FF0000", "#00FF00" vb.
        /// </summary>
        public string? ColorCode { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Bağlı olduğu seçenek türü
        /// </summary>
        public virtual ProductOption Option { get; set; } = null!;

        /// <summary>
        /// Bu değeri kullanan varyant ilişkileri
        /// Many-to-Many ilişki: VariantOptionValue üzerinden
        /// </summary>
        public virtual ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new HashSet<VariantOptionValue>();

        #endregion
    }
}
