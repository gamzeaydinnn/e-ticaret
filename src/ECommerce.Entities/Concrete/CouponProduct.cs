// =============================================================================
// CouponProduct Entity - Kupon-Ürün İlişki Tablosu (Junction Table)
// =============================================================================
// Bu entity, kuponların hangi ürünlerde geçerli olduğunu tanımlar.
// Many-to-many ilişki için ara tablo görevi görür.
// Eğer bir kuponun CouponProducts koleksiyonu boşsa, kupon tüm ürünlerde geçerlidir.
// =============================================================================

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Kupon-Ürün many-to-many ilişki tablosu.
    /// Hangi kuponların hangi ürünlerde geçerli olduğunu tanımlar.
    /// </summary>
    public class CouponProduct
    {
        // =============================================================================
        // Composite Primary Key (Fluent API ile tanımlanacak)
        // =============================================================================

        /// <summary>
        /// Kupon ID - Foreign Key
        /// </summary>
        public int CouponId { get; set; }

        /// <summary>
        /// Ürün ID - Foreign Key
        /// </summary>
        public int ProductId { get; set; }

        // =============================================================================
        // Navigation Properties
        // =============================================================================

        /// <summary>
        /// İlişkili kupon
        /// </summary>
        [ForeignKey("CouponId")]
        public virtual Coupon Coupon { get; set; } = null!;

        /// <summary>
        /// İlişkili ürün
        /// </summary>
        [ForeignKey("ProductId")]
        public virtual Product Product { get; set; } = null!;

        // =============================================================================
        // Ek Bilgiler (opsiyonel)
        // =============================================================================

        /// <summary>
        /// Bu ürün için özel indirim değeri (opsiyonel)
        /// null ise kuponun varsayılan değeri kullanılır
        /// </summary>
        public decimal? CustomDiscountValue { get; set; }

        /// <summary>
        /// Kayıt oluşturulma tarihi
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
