using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;

namespace ECommerce.API.Authorization
{
    /// <summary>
    /// Permission bazlı yetkilendirme handler'ı.
    /// Her HTTP request'inde kullanıcının gerekli izne sahip olup olmadığını kontrol eder.
    /// 
    /// Çalışma Prensibi:
    /// 1. JWT token'dan kullanıcı ID'si alınır
    /// 2. PermissionService üzerinden izinler sorgulanır
    /// 3. Gerekli izin varsa Succeed, yoksa Fail döner
    /// 
    /// Performans Optimizasyonu:
    /// - PermissionService cache kullanır (5 dk)
    /// - SuperAdmin/Admin için hızlı yol (tüm izinler)
    /// 
    /// Güvenlik Notları:
    /// - Authenticate olmamış kullanıcılar reddedilir
    /// - Claim'lerden userId alınamazsa reddedilir
    /// </summary>
    public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
    {
        private readonly IPermissionService _permissionService;
        private readonly ILogger<PermissionAuthorizationHandler> _logger;

        public PermissionAuthorizationHandler(
            IPermissionService permissionService,
            ILogger<PermissionAuthorizationHandler> logger)
        {
            _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Yetkilendirme kontrolü yapar.
        /// </summary>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            PermissionRequirement requirement)
        {
            // 1. Kullanıcı authenticate olmuş mu?
            if (context.User?.Identity?.IsAuthenticated != true)
            {
                _logger.LogDebug("Kullanıcı authenticate değil, izin reddedildi");
                return; // Fail - context.Fail() çağırmıyoruz, diğer handler'lara şans ver
            }

            // 2. Kullanıcı ID'sini al
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? context.User.FindFirst("sub")?.Value
                           ?? context.User.FindFirst("nameid")?.Value;

            if (string.IsNullOrWhiteSpace(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                _logger.LogWarning("Kullanıcı ID claim'i bulunamadı veya geçersiz");
                return;
            }

            // 3. SuperAdmin/Admin kontrolü - hızlı yol
            // Bu roller tüm izinlere sahip, DB sorgusu yapmaya gerek yok
            if (context.User.IsInRole(Roles.SuperAdmin) || context.User.IsInRole(Roles.Admin))
            {
                _logger.LogDebug("SuperAdmin/Admin kullanıcısı, izin verildi: {Permission}", requirement.Permission);
                context.Succeed(requirement);
                return;
            }

            try
            {
                // 4. İzin kontrolü yap
                var hasPermission = await _permissionService.UserHasPermissionAsync(userId, requirement.Permission);

                if (hasPermission)
                {
                    _logger.LogDebug("İzin verildi: UserId={UserId}, Permission={Permission}", userId, requirement.Permission);
                    context.Succeed(requirement);
                }
                else
                {
                    _logger.LogDebug("İzin reddedildi: UserId={UserId}, Permission={Permission}", userId, requirement.Permission);
                    // context.Fail() çağırmıyoruz - sadece Succeed çağırmamak yeterli
                }
            }
            catch (Exception ex)
            {
                // Hata durumunda güvenlik için izin verme
                _logger.LogError(ex, "İzin kontrolü sırasında hata: UserId={UserId}, Permission={Permission}", 
                    userId, requirement.Permission);
                // Fail - sessizce devam et
            }
        }
    }
}
