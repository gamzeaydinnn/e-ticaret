using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ECommerce.Entities.Concrete
{
    /// <summary>
    /// Sistemdeki tüm izinleri (permissions) temsil eden entity.
    /// RBAC (Role-Based Access Control) sisteminin temel yapı taşıdır.
    /// 
    /// İzinler modül bazlı gruplandırılır ve roller aracılığıyla kullanıcılara atanır.
    /// Örnek: "products.create" izni, ürün oluşturma yetkisini temsil eder.
    /// 
    /// Neden BaseEntity'den türetildi:
    /// - Projedeki diğer entity'lerle tutarlılık sağlamak için
    /// - Id, IsActive, CreatedAt, UpdatedAt alanlarını otomatik almak için
    /// </summary>
    public class Permission : BaseEntity
    {
        /// <summary>
        /// İznin kod içinde kullanılan benzersiz adı.
        /// Format: "modül.aksiyon" (örn: "products.create", "orders.view")
        /// 
        /// Neden bu format:
        /// - Programatik erişim için tutarlı bir yapı sağlar
        /// - Frontend ve backend'de aynı format kullanılarak sync sağlanır
        /// - Modül bazlı gruplama kolaylaşır
        /// </summary>
        [Required(ErrorMessage = "Permission adı zorunludur")]
        [MaxLength(100, ErrorMessage = "Permission adı en fazla 100 karakter olabilir")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Admin panelinde gösterilecek kullanıcı dostu isim.
        /// Örnek: "Ürün Oluşturma", "Sipariş Görüntüleme"
        /// </summary>
        [Required(ErrorMessage = "Görüntülenecek ad zorunludur")]
        [MaxLength(200, ErrorMessage = "Görüntülenecek ad en fazla 200 karakter olabilir")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// İznin ne işe yaradığını açıklayan detaylı metin.
        /// Admin panelinde tooltip veya yardım metni olarak kullanılır.
        /// </summary>
        [MaxLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
        public string? Description { get; set; }

        /// <summary>
        /// İznin ait olduğu modül/kategori.
        /// Örnek: "Products", "Orders", "Users", "Settings"
        /// 
        /// Neden gerekli:
        /// - Admin panelinde izinleri gruplamak için
        /// - Filtreleme ve arama işlemleri için
        /// - Yetki matrisi oluştururken kategorilendirme için
        /// </summary>
        [Required(ErrorMessage = "Modül adı zorunludur")]
        [MaxLength(100, ErrorMessage = "Modül adı en fazla 100 karakter olabilir")]
        public string Module { get; set; } = string.Empty;

        /// <summary>
        /// Admin panelinde izinlerin sıralanması için kullanılır.
        /// Düşük değer = üstte görünür.
        /// </summary>
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// Bu izne sahip olan rol-permission ilişkileri.
        /// Navigation property - Lazy loading için virtual.
        /// 
        /// Many-to-Many ilişki:
        /// Permission (1) <--> (N) RolePermission (N) <--> (1) Role
        /// </summary>
        public virtual ICollection<RolePermission> RolePermissions { get; set; } = new HashSet<RolePermission>();

        #region Equality & Hashing

        /// <summary>
        /// İki Permission'ın eşitliğini Name bazında kontrol eder.
        /// Neden: Name alanı unique olduğundan, karşılaştırma için yeterlidir.
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (obj is Permission other)
            {
                return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// HashCode üretimi Name bazında yapılır.
        /// Dictionary ve HashSet kullanımlarında tutarlılık sağlar.
        /// </summary>
        public override int GetHashCode()
        {
            return Name?.ToLowerInvariant().GetHashCode() ?? 0;
        }

        #endregion
    }
}
