using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace ECommerce.API.Authorization
{
    /// <summary>
    /// Dinamik permission policy provider.
    /// "Permission:xxx" formatındaki policy'leri otomatik oluşturur.
    /// 
    /// Neden gerekli:
    /// - Her permission için manuel policy tanımı yapmak gerekmez
    /// - Runtime'da policy oluşturulur
    /// - Yeni permission eklendiğinde kod değişikliği gerekmez
    /// 
    /// Çalışma Prensibi:
    /// 1. Policy adı "Permission:" ile başlıyorsa
    /// 2. Permission adını parse et
    /// 3. PermissionRequirement ile yeni policy oluştur
    /// 4. PermissionAuthorizationHandler bu policy'yi işler
    /// </summary>
    public class PermissionPolicyProvider : IAuthorizationPolicyProvider
    {
        private readonly DefaultAuthorizationPolicyProvider _fallbackPolicyProvider;

        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        {
            // Standart policy'ler için fallback provider
            _fallbackPolicyProvider = new DefaultAuthorizationPolicyProvider(options);
        }

        /// <summary>
        /// Varsayılan policy'yi döner.
        /// [Authorize] attribute'u policy belirtmeden kullanıldığında çağrılır.
        /// </summary>
        public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
        {
            return _fallbackPolicyProvider.GetDefaultPolicyAsync();
        }

        /// <summary>
        /// Fallback policy'yi döner.
        /// Authorization middleware tarafından kullanılır.
        /// </summary>
        public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
        {
            return _fallbackPolicyProvider.GetFallbackPolicyAsync();
        }

        /// <summary>
        /// Policy adına göre policy döner.
        /// "Permission:" prefix'i ile başlayan policy'ler dinamik oluşturulur.
        /// </summary>
        public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            // Permission policy mi kontrol et
            if (policyName.StartsWith(HasPermissionAttribute.PolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Permission adını çıkar
                var permission = policyName.Substring(HasPermissionAttribute.PolicyPrefix.Length);

                // Dinamik policy oluştur
                var policy = new AuthorizationPolicyBuilder()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            // Standart policy'ler için fallback kullan
            return _fallbackPolicyProvider.GetPolicyAsync(policyName);
        }
    }
}
