using Microsoft.AspNetCore.Authorization;

namespace ECommerce.API.Authorization
{
    /// <summary>
    /// Controller action'larına permission bazlı yetkilendirme eklemek için attribute.
    /// Standart [Authorize] attribute'unun permission-aware versiyonu.
    /// 
    /// Kullanım Örnekleri:
    /// 
    /// // Tekil izin kontrolü
    /// [HasPermission(Permissions.Products.Create)]
    /// public async Task<IActionResult> CreateProduct(...)
    /// 
    /// // String ile kullanım
    /// [HasPermission("products.create")]
    /// public async Task<IActionResult> CreateProduct(...)
    /// 
    /// // Controller seviyesinde
    /// [HasPermission("products.view")]
    /// public class AdminProductsController : ControllerBase
    /// 
    /// Neden AuthorizeAttribute'dan türetildi:
    /// - ASP.NET Core'un standart authorization pipeline'ını kullanır
    /// - Swagger/OpenAPI ile uyumlu
    /// - Diğer authorization attribute'ları ile birlikte çalışabilir
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
    public class HasPermissionAttribute : AuthorizeAttribute
    {
        /// <summary>
        /// Permission policy prefix.
        /// Policy adları "Permission:products.create" formatında oluşturulur.
        /// </summary>
        public const string PolicyPrefix = "Permission:";

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="permission">Gereken izin adı (örn: "products.create")</param>
        public HasPermissionAttribute(string permission)
            : base(PolicyPrefix + permission)
        {
            Permission = permission;
        }

        /// <summary>
        /// Bu attribute için gereken izin.
        /// </summary>
        public string Permission { get; }
    }
}
