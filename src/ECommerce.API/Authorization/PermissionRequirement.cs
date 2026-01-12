using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Authorization
{
    /// <summary>
    /// Permission bazlı yetkilendirme için requirement sınıfı.
    /// ASP.NET Core Authorization Policy sistemine entegre olur.
    /// 
    /// Kullanım:
    /// [Authorize(Policy = "Permission:products.create")]
    /// veya
    /// [HasPermission("products.create")]
    /// 
    /// Neden IAuthorizationRequirement:
    /// - ASP.NET Core'un standart yetkilendirme mekanizması
    /// - Handler'lar ile gevşek bağlılık
    /// - Birden fazla handler destekler
    /// </summary>
    public class PermissionRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// Gereken izin adı.
        /// Örnek: "products.create", "orders.view"
        /// </summary>
        public string Permission { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="permission">Gereken izin adı</param>
        public PermissionRequirement(string permission)
        {
            Permission = permission ?? throw new ArgumentNullException(nameof(permission));
        }
    }
}
