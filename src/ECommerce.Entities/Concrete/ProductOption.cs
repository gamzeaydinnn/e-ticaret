// ProductOption: Ürün seçenek türlerini tanımlar.
// Örnek seçenek türleri: "Hacim", "Renk", "Beden", "Paket", "Malzeme"
// Her seçenek türü birden fazla değere sahip olabilir (ProductOptionValue).
// Bu yapı, esnek varyant sistemi için temel oluşturur.

using System;
using System.Collections.Generic;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Ürün seçenek türü entity'si.
    /// Varyantları tanımlamak için kullanılan özellik kategorileri.
    /// Örn: Hacim, Renk, Beden, Paket Tipi
    /// </summary>
    public class ProductOption : BaseEntity
    {
        #region Temel Alanlar

        /// <summary>
        /// Seçenek adı (UNIQUE olmalı)
        /// Örn: "Hacim", "Renk", "Beden"
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Görüntüleme sırası
        /// UI'da seçeneklerin hangi sırada gösterileceğini belirler
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Açıklama (opsiyonel)
        /// Admin panelinde yardımcı bilgi olarak gösterilebilir
        /// </summary>
        public string? Description { get; set; }

        #endregion

        #region Navigation Properties

        /// <summary>
        /// Bu seçenek türüne ait değerler
        /// Örn: Hacim seçeneği için: 330ml, 500ml, 1L, 2L
        /// </summary>
        public virtual ICollection<ProductOptionValue> OptionValues { get; set; } = new HashSet<ProductOptionValue>();

        #endregion
    }
}
