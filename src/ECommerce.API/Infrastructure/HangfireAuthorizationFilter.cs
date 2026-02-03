using Hangfire.Dashboard;
using Hangfire.Annotations;
using Microsoft.AspNetCore.Http;

namespace ECommerce.API.Infrastructure;

/// <summary>
/// Hangfire Dashboard için yetkilendirme filtresi.
/// Sadece yetkili kullanıcıların (Admin) dashboard'a erişmesini sağlar.
/// </summary>
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    /// <summary>
    /// Dashboard erişim kontrolü.
    /// Development ortamında herkese açık, production'da Admin gerektirir.
    /// </summary>
    public bool Authorize([NotNull] DashboardContext context)
    {
        // ASP.NET Core HttpContext'i al
        var httpContext = context.GetHttpContext();
        if (httpContext == null)
        {
            return false;
        }
        
        // Development ortamında herkes erişebilir
        var environment = httpContext.RequestServices.GetService<IHostEnvironment>();
        if (environment?.IsDevelopment() == true)
        {
            return true;
        }
        
        // Production'da authenticated + Admin rolü gerekli
        if (!httpContext.User.Identity?.IsAuthenticated ?? true)
        {
            return false;
        }
        
        // Admin veya SystemAdmin rolü kontrolü
        return httpContext.User.IsInRole("Admin") || 
               httpContext.User.IsInRole("SystemAdmin");
    }
}
