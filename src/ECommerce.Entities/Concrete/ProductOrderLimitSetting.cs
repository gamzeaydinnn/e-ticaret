using System;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Site genelinde geçerli sipariş miktar limiti varsayılanları (singleton).
    /// Ürün/varyant override yoksa bu değerler uygulanır.
    /// </summary>
    public class ProductOrderLimitSetting : BaseEntity
    {
        /// <summary>Adet bazlı ürünler için varsayılan maksimum miktar.</summary>
        public int DefaultMaxQuantityPiece { get; set; } = 5;

        /// <summary>Adet bazlı ürünler için varsayılan minimum miktar.</summary>
        public int DefaultMinQuantityPiece { get; set; } = 1;

        /// <summary>Adet bazlı ürünler için varsayılan artış adımı.</summary>
        public decimal DefaultQuantityStepPiece { get; set; } = 1m;

        /// <summary>Kg bazlı ürünler için varsayılan maksimum ağırlık (kg).</summary>
        public decimal DefaultMaxWeightKg { get; set; } = 10m;

        /// <summary>Kg bazlı ürünler için varsayılan minimum ağırlık (kg).</summary>
        public decimal DefaultMinWeightKg { get; set; } = 0.25m;

        /// <summary>Kg bazlı ürünler için varsayılan artış adımı (kg).</summary>
        public decimal DefaultWeightStepKg { get; set; } = 0.25m;

        public int? UpdatedByUserId { get; set; }
        public string? UpdatedByUserName { get; set; }
    }
}
