using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Rol ve Permission arasındaki many-to-many ilişkiyi yöneten join tablosu.
    /// ASP.NET Identity'nin AspNetRoles tablosu ile Permissions tablosunu bağlar.
    /// 
    /// Neden BaseEntity'den türetilmedi:
    /// - Join tabloları genellikle sadece foreign key'leri tutar
    /// - IsActive ve UpdatedAt gibi alanlar bu ilişki için gereksizdir
    /// - Ancak CreatedAt audit için eklendi
    /// 
    /// İlişki yapısı:
    /// AspNetRoles (1) <--> (N) RolePermissions (N) <--> (1) Permissions
    /// </summary>
    public class RolePermission
    {
        /// <summary>
        /// Primary key - Auto increment.
        /// Composite key yerine surrogate key tercih edildi çünkü:
        /// - EF Core'da daha kolay yönetim
        /// - Gelecekteki değişikliklere esneklik
        /// </summary>
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// ASP.NET Identity Role Id.
        /// AspNetRoles tablosundaki Id'ye referans verir.
        /// 
        /// NOT: IdentityRole<int> kullanıldığından int tipinde.
        /// </summary>
        [Required]
        public int RoleId { get; set; }

        /// <summary>
        /// Permission entity Id.
        /// Permissions tablosundaki Id'ye referans verir.
        /// </summary>
        [Required]
        public int PermissionId { get; set; }

        /// <summary>
        /// Bu iznin role ne zaman atandığı.
        /// Audit ve log amaçlı kullanılır.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Bu izni hangi kullanıcının atadığı.
        /// Audit trail için önemli - null ise sistem tarafından oluşturulmuş demektir.
        /// </summary>
        public int? CreatedByUserId { get; set; }

        #region Navigation Properties

        /// <summary>
        /// İlişkili Permission entity.
        /// Lazy loading için virtual işaretlendi.
        /// </summary>
        public virtual Permission Permission { get; set; } = null!;

        /// <summary>
        /// İzni atayan kullanıcı (opsiyonel).
        /// Seed data için null olabilir.
        /// </summary>
        [ForeignKey(nameof(CreatedByUserId))]
        public virtual User? CreatedByUser { get; set; }

        #endregion

        #region Equality & Hashing

        /// <summary>
        /// İki RolePermission'ın eşitliğini RoleId + PermissionId bazında kontrol eder.
        /// Aynı role aynı izin birden fazla kez atanamaz.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is RolePermission other)
            {
                return RoleId == other.RoleId && PermissionId == other.PermissionId;
            }
            return false;
        }

        /// <summary>
        /// HashCode üretimi RoleId ve PermissionId kombinasyonuna göre yapılır.
        /// </summary>
        public override int GetHashCode()
        {
            return HashCode.Combine(RoleId, PermissionId);
        }

        #endregion
    }
}
