using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Constants;
using ECommerce.Data.Context;

namespace ECommerce.API.Infrastructure
{
    /// <summary>
    /// Uygulama başlangıcında Identity verilerini seed eder.
    /// - Roller (SuperAdmin, StoreManager, CustomerSupport, Logistics, User)
    /// - Varsayılan admin kullanıcı
    /// - Permission tanımları
    /// - Rol-Permission atamaları
    /// 
    /// Neden ayrı bir seeder:
    /// - Program.cs'i temiz tutmak için
    /// - Seed mantığını modüler ve test edilebilir kılmak için
    /// - Migration'dan bağımsız olarak çalışabilmesi için
    /// </summary>
    public static class IdentitySeeder
    {
        /// <summary>
        /// Tüm seed işlemlerini sırayla gerçekleştirir.
        /// Transaction içinde çalışır - hata olursa geri alınır.
        /// </summary>
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = services.GetRequiredService<UserManager<User>>();
            var config = services.GetRequiredService<IConfiguration>();
            var dbContext = services.GetRequiredService<ECommerceDbContext>();
            var logger = services.GetService<ILogger<Program>>(); // Opsiyonel loglama

            try
            {
                // 1. Rolleri oluştur
                await SeedRolesAsync(roleManager, logger);

                // 2. Varsayılan admin kullanıcıyı oluştur
                await SeedAdminUserAsync(userManager, config, logger);

                // 3. Permission'ları seed et
                await SeedPermissionsAsync(dbContext, logger);

                // 4. Role-Permission atamalarını yap
                await SeedRolePermissionsAsync(dbContext, roleManager, logger);

                logger?.LogInformation("✅ IdentitySeeder tüm işlemleri başarıyla tamamladı");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "❌ IdentitySeeder sırasında hata oluştu");
                throw; // Hata yukarı fırlatılır - uygulama başlatılmamalı
            }
        }

        /// <summary>
        /// Sistemdeki tüm rolleri oluşturur.
        /// Mevcut roller varsa atlanır (idempotent).
        /// </summary>
        private static async Task SeedRolesAsync(RoleManager<IdentityRole<int>> roleManager, ILogger? logger)
        {
            // Tüm rolleri al
            string[] roles = Core.Constants.Roles.GetAllRoles();

            foreach (var roleName in roles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    var result = await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                    if (result.Succeeded)
                    {
                        logger?.LogInformation("✅ Rol oluşturuldu: {RoleName}", roleName);
                    }
                    else
                    {
                        logger?.LogWarning("⚠️ Rol oluşturulamadı: {RoleName} - {Errors}", 
                            roleName, string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }
        }

        /// <summary>
        /// Varsayılan admin kullanıcıyı oluşturur veya günceller.
        /// Konfigürasyondan email ve şifre alır.
        /// </summary>
        private static async Task SeedAdminUserAsync(
            UserManager<User> userManager, 
            IConfiguration config, 
            ILogger? logger)
        {
            var adminEmail = config["Admin:Email"] ?? "admin@admin.com";
            var adminPassword = config["Admin:Password"] ?? "admin123";

            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            
            if (adminUser == null)
            {
                // Yeni admin kullanıcı oluştur
                adminUser = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FirstName = "Süper",
                    LastName = "Yönetici",
                    FullName = "Süper Yönetici",
                    EmailConfirmed = true,
                    IsActive = true,
                    Role = Core.Constants.Roles.SuperAdmin,
                    CreatedAt = DateTime.UtcNow
                };

                var createResult = await userManager.CreateAsync(adminUser, adminPassword);
                if (createResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Core.Constants.Roles.SuperAdmin);
                    logger?.LogInformation("✅ Admin kullanıcı oluşturuldu: {Email}", adminEmail);
                }
                else
                {
                    logger?.LogError("❌ Admin kullanıcı oluşturulamadı: {Errors}", 
                        string.Join(", ", createResult.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                // Mevcut admin kullanıcıyı güncelle (gerekirse)
                var requiresUpdate = false;
                
                if (!adminUser.EmailConfirmed)
                {
                    adminUser.EmailConfirmed = true;
                    requiresUpdate = true;
                }
                if (!adminUser.IsActive)
                {
                    adminUser.IsActive = true;
                    requiresUpdate = true;
                }
                if (adminUser.Role != Core.Constants.Roles.SuperAdmin)
                {
                    adminUser.Role = Core.Constants.Roles.SuperAdmin;
                    requiresUpdate = true;
                }

                if (requiresUpdate)
                {
                    await userManager.UpdateAsync(adminUser);
                    logger?.LogInformation("✅ Admin kullanıcı güncellendi: {Email}", adminEmail);
                }

                // Role atamasını kontrol et
                if (!await userManager.IsInRoleAsync(adminUser, Core.Constants.Roles.SuperAdmin))
                {
                    await userManager.AddToRoleAsync(adminUser, Core.Constants.Roles.SuperAdmin);
                }
            }
        }

        /// <summary>
        /// Tüm permission'ları veritabanına ekler.
        /// Mevcut permission'lar güncellenir (DisplayName, Description).
        /// </summary>
        private static async Task SeedPermissionsAsync(ECommerceDbContext dbContext, ILogger? logger)
        {
            var allPermissions = Permissions.GetAllPermissions().ToList();
            var existingPermissions = await dbContext.Permissions
                .ToDictionaryAsync(p => p.Name, StringComparer.OrdinalIgnoreCase);

            var sortOrder = 0;
            var addedCount = 0;
            var updatedCount = 0;

            foreach (var (name, module) in allPermissions)
            {
                sortOrder++;
                var displayName = Permissions.GetDisplayName(name);

                if (existingPermissions.TryGetValue(name, out var existing))
                {
                    // Mevcut permission'ı güncelle (gerekirse)
                    var needsUpdate = false;
                    
                    if (existing.DisplayName != displayName)
                    {
                        existing.DisplayName = displayName;
                        needsUpdate = true;
                    }
                    if (existing.Module != module)
                    {
                        existing.Module = module;
                        needsUpdate = true;
                    }
                    if (existing.SortOrder != sortOrder)
                    {
                        existing.SortOrder = sortOrder;
                        needsUpdate = true;
                    }
                    if (!existing.IsActive)
                    {
                        existing.IsActive = true;
                        needsUpdate = true;
                    }

                    if (needsUpdate)
                    {
                        existing.UpdatedAt = DateTime.UtcNow;
                        updatedCount++;
                    }
                }
                else
                {
                    // Yeni permission ekle
                    var permission = new Permission
                    {
                        Name = name,
                        DisplayName = displayName,
                        Description = $"{module} modülü için {displayName.Split(' ').LastOrDefault()} yetkisi",
                        Module = module,
                        SortOrder = sortOrder,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    dbContext.Permissions.Add(permission);
                    addedCount++;
                }
            }

            if (addedCount > 0 || updatedCount > 0)
            {
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("✅ Permissions seed edildi: {Added} eklendi, {Updated} güncellendi", 
                    addedCount, updatedCount);
            }
        }

        /// <summary>
        /// Her role uygun izinleri atar.
        /// "En Az Yetki" (Least Privilege) prensibi uygulanır.
        /// </summary>
        private static async Task SeedRolePermissionsAsync(
            ECommerceDbContext dbContext, 
            RoleManager<IdentityRole<int>> roleManager,
            ILogger? logger)
        {
            // Tüm permission'ları ve rolleri çek
            var permissions = await dbContext.Permissions
                .Where(p => p.IsActive)
                .ToDictionaryAsync(p => p.Name, p => p.Id, StringComparer.OrdinalIgnoreCase);
            
            var roles = await roleManager.Roles.ToDictionaryAsync(r => r.Name!, r => r.Id);

            // Mevcut atamalar
            var existingAssignments = await dbContext.RolePermissions
                .Select(rp => new { rp.RoleId, rp.PermissionId })
                .ToHashSetAsync();

            // Her rol için izin atamaları
            var rolePermissionMap = GetRolePermissionMap();
            var addedCount = 0;

            foreach (var (roleName, permissionNames) in rolePermissionMap)
            {
                if (!roles.TryGetValue(roleName, out var roleId))
                {
                    logger?.LogWarning("⚠️ Rol bulunamadı: {RoleName}", roleName);
                    continue;
                }

                foreach (var permissionName in permissionNames)
                {
                    if (!permissions.TryGetValue(permissionName, out var permissionId))
                    {
                        logger?.LogWarning("⚠️ Permission bulunamadı: {PermissionName}", permissionName);
                        continue;
                    }

                    // Zaten atanmış mı kontrol et
                    var assignmentKey = new { RoleId = roleId, PermissionId = permissionId };
                    if (existingAssignments.Any(e => e.RoleId == roleId && e.PermissionId == permissionId))
                        continue;

                    // Yeni atama ekle
                    dbContext.RolePermissions.Add(new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = permissionId,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = null // Sistem tarafından oluşturuldu
                    });
                    addedCount++;
                }
            }

            if (addedCount > 0)
            {
                await dbContext.SaveChangesAsync();
                logger?.LogInformation("✅ RolePermissions seed edildi: {Added} atama eklendi", addedCount);
            }
        }

        /// <summary>
        /// Her rol için atanacak izinleri tanımlar.
        /// Bu yapı "En Az Yetki" prensibine göre tasarlanmıştır.
        /// </summary>
        private static Dictionary<string, string[]> GetRolePermissionMap()
        {
            return new Dictionary<string, string[]>
            {
                // SuperAdmin: TÜM İZİNLER
                [Core.Constants.Roles.SuperAdmin] = Permissions.GetAllPermissions()
                    .Select(p => p.Name)
                    .ToArray(),

                // Admin: SuperAdmin ile aynı (geriye dönük uyumluluk)
                [Core.Constants.Roles.Admin] = Permissions.GetAllPermissions()
                    .Select(p => p.Name)
                    .ToArray(),

                // StoreManager: Ürün, kategori, kampanya, stok ve raporlar
                [Core.Constants.Roles.StoreManager] = new[]
                {
                    // Dashboard
                    Permissions.Dashboard.View,
                    Permissions.Dashboard.ViewStatistics,
                    Permissions.Dashboard.ViewRevenueChart,
                    
                    // Ürünler - Tam yetki
                    Permissions.Products.View,
                    Permissions.Products.Create,
                    Permissions.Products.Update,
                    Permissions.Products.Delete,
                    Permissions.Products.ManageStock,
                    Permissions.Products.ManagePricing,
                    Permissions.Products.Import,
                    Permissions.Products.Export,
                    
                    // Kategoriler - Tam yetki
                    Permissions.Categories.View,
                    Permissions.Categories.Create,
                    Permissions.Categories.Update,
                    Permissions.Categories.Delete,
                    
                    // Siparişler - Görüntüleme ve güncelleme
                    Permissions.Orders.View,
                    Permissions.Orders.ViewDetails,
                    Permissions.Orders.UpdateStatus,
                    Permissions.Orders.ViewCustomerInfo,
                    Permissions.Orders.Export,
                    
                    // Kampanyalar - Tam yetki
                    Permissions.Campaigns.View,
                    Permissions.Campaigns.Create,
                    Permissions.Campaigns.Update,
                    Permissions.Campaigns.Delete,
                    
                    // Kuponlar - Tam yetki
                    Permissions.Coupons.View,
                    Permissions.Coupons.Create,
                    Permissions.Coupons.Update,
                    Permissions.Coupons.Delete,
                    
                    // Markalar - Tam yetki
                    Permissions.Brands.View,
                    Permissions.Brands.Create,
                    Permissions.Brands.Update,
                    Permissions.Brands.Delete,
                    
                    // Bannerlar - Tam yetki
                    Permissions.Banners.View,
                    Permissions.Banners.Create,
                    Permissions.Banners.Update,
                    Permissions.Banners.Delete,
                    
                    // Raporlar - Satış ve envanter
                    Permissions.Reports.View,  // Genel rapor görüntüleme
                    Permissions.Reports.ViewSales,
                    Permissions.Reports.ViewInventory,
                    Permissions.Reports.Export,
                    
                    // ============================================================================
                    // KULLANICI YÖNETİMİ - KALDIRILDI
                    // StoreManager kullanıcı yönetimine erişmemeli
                    // Sadece ürün, kategori, kampanya ve raporlarla ilgilenebilir
                    // ============================================================================
                    // Permissions.Users.View,  // ❌ KALDIRILDI
                    
                    // Kuryeler - Görüntüleme
                    Permissions.Couriers.View
                },

                // CustomerSupport: Sipariş yönetimi, iade, müşteri iletişimi
                [Core.Constants.Roles.CustomerSupport] = new[]
                {
                    // Dashboard - Sadece görüntüleme
                    Permissions.Dashboard.View,
                    
                    // Ürünler - Sadece görüntüleme
                    Permissions.Products.View,
                    
                    // Kategoriler - Sadece görüntüleme
                    Permissions.Categories.View,
                    
                    // Siparişler - Tam yetki (iptal/iade dahil)
                    Permissions.Orders.View,
                    Permissions.Orders.ViewDetails,
                    Permissions.Orders.UpdateStatus,
                    Permissions.Orders.Cancel,
                    Permissions.Orders.ProcessRefund,
                    Permissions.Orders.ViewCustomerInfo,
                    
                    // Kullanıcılar - Sadece görüntüleme (hassas veri hariç)
                    Permissions.Users.View,
                    
                    // Raporlar - Genel görüntüleme ve satış
                    Permissions.Reports.View,
                    Permissions.Reports.ViewSales
                },

                // Logistics: Kargo ve teslimat operasyonları
                [Core.Constants.Roles.Logistics] = new[]
                {
                    // Dashboard - Sadece görüntüleme
                    Permissions.Dashboard.View,
                    
                    // Siparişler - Sınırlı erişim (müşteri bilgisi YOK)
                    Permissions.Orders.View,
                    Permissions.Orders.UpdateStatus,
                    
                    // Kargo/Teslimat - Tam yetki
                    Permissions.Shipping.ViewPendingShipments,
                    Permissions.Shipping.UpdateTrackingNumber,
                    Permissions.Shipping.MarkAsShipped,
                    Permissions.Shipping.MarkAsDelivered,
                    
                    // Kuryeler - Görüntüleme ve atama
                    Permissions.Couriers.View,
                    Permissions.Couriers.AssignOrders,
                    
                    // Raporlar - Genel görüntüleme ve ağırlık raporları
                    Permissions.Reports.View,
                    Permissions.Reports.ViewWeight
                },

                // User/Customer: Müşteri izinleri (admin paneli erişimi yok)
                [Core.Constants.Roles.User] = Array.Empty<string>(),
                [Core.Constants.Roles.Customer] = Array.Empty<string>()
            };
        }
    }
}
