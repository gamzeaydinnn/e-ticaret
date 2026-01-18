// VariantOptionValue: Varyant ile seçenek değerleri arasındaki Many-to-Many ilişkiyi temsil eder.
// Her varyant birden fazla seçenek değerine sahip olabilir (Hacim=330ml, Paket=6'lı).
// Composite Primary Key: VariantId + OptionValueId
// Bu tablo, varyantların hangi özelliklere sahip olduğunu tanımlar.

using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Varyant-SeçenekDeğeri ilişki tablosu.
    /// Composite PK kullanır, BaseEntity'den türemez (kendi Id'si yok).
    /// Örnek: Varyant(SKU=COCA330) → OptionValue(Hacim=330ml)
    /// </summary>
    public class VariantOptionValue
    {
        #region Composite Primary Key

        /// <summary>
        /// Varyant ID'si (Composite PK parçası, Foreign Key)
        /// </summary>
        public int VariantId { get; set; }

        /// <summary>
        /// Seçenek değeri ID'si (Composite PK parçası, Foreign Key)
        /// </summary>
        public int OptionValueId { get; set; }

        #endregion

        #region Metadata

        /// <summary>
        /// Kayıt oluşturulma zamanı
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        #endregion

        #region Navigation Properties

        /// <summary>
        /// İlişkili varyant
        /// </summary>
        public virtual ProductVariant Variant { get; set; } = null!;

        /// <summary>
        /// İlişkili seçenek değeri
        /// </summary>
        public virtual ProductOptionValue OptionValue { get; set; } = null!;

        #endregion
    }
}
