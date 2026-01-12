// =============================================================================
// AdminPermissionsController.cs - RBAC İzin Yönetimi Controller'ı
// =============================================================================
// Bu controller, sistemdeki izinlerin görüntülenmesi ve yönetilmesi için
// API endpoint'lerini sağlar. Sadece SuperAdmin rolüne sahip kullanıcılar
// erişebilir.
// =============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.API.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// Sistemdeki izinlerin yönetimi için API endpoint'leri.
    /// Bu controller sadece SuperAdmin tarafından kullanılabilir.
    /// </summary>
    [ApiController]
    [Authorize(Roles = Roles.SuperAdmin)]
    [Route("api/admin/permissions")]
    public class AdminPermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;
        private readonly IAuditLogService _auditLogService;

        public AdminPermissionsController(
            IPermissionService permissionService,
            IAuditLogService auditLogService)
        {
            _permissionService = permissionService;
            _auditLogService = auditLogService;
        }

        // =====================================================================
        // GET: api/admin/permissions
        // Tüm izinleri listeler (modüle göre gruplu veya düz liste)
        // =====================================================================
        /// <summary>
        /// Sistemdeki tüm izinleri getirir.
        /// </summary>
        /// <param name="groupByModule">True ise izinleri modüle göre gruplar</param>
        /// <returns>İzin listesi</returns>
        [HttpGet]
        public async Task<IActionResult> GetAllPermissions([FromQuery] bool groupByModule = false)
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionsAsync();

                if (groupByModule)
                {
                    // Modüle göre grupla
                    var grouped = permissions
                        .GroupBy(p => p.Module)
                        .Select(g => new
                        {
                            Module = g.Key,
                            ModuleDisplayName = GetModuleDisplayName(g.Key),
                            Permissions = g.OrderBy(p => p.SortOrder).Select(p => new
                            {
                                p.Id,
                                p.Name,
                                p.DisplayName,
                                p.Description,
                                p.IsActive
                            }).ToList()
                        })
                        .OrderBy(g => g.Module)
                        .ToList();

                    return Ok(new
                    {
                        success = true,
                        data = grouped,
                        totalCount = permissions.Count()
                    });
                }
                else
                {
                    // Düz liste
                    var list = permissions.Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.DisplayName,
                        p.Description,
                        p.Module,
                        p.SortOrder,
                        p.IsActive
                    }).ToList();

                    return Ok(new
                    {
                        success = true,
                        data = list,
                        count = list.Count
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/permissions/{id}
        // Belirli bir iznin detaylarını getirir
        // =====================================================================
        /// <summary>
        /// Belirtilen ID'ye sahip iznin detaylarını getirir.
        /// </summary>
        /// <param name="id">İzin ID'si</param>
        /// <returns>İzin detayları</returns>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetPermissionById(int id)
        {
            try
            {
                var permission = await _permissionService.GetPermissionByIdAsync(id);
                if (permission == null)
                {
                    return NotFound(new { success = false, message = "İzin bulunamadı." });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        permission.Id,
                        permission.Name,
                        permission.DisplayName,
                        permission.Description,
                        permission.Module,
                        permission.SortOrder,
                        permission.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/permissions/by-name/{name}
        // İzni adına göre getirir
        // =====================================================================
        /// <summary>
        /// Belirtilen isme sahip iznin detaylarını getirir.
        /// </summary>
        /// <param name="name">İzin adı (örn: "products.create")</param>
        /// <returns>İzin detayları</returns>
        [HttpGet("by-name/{name}")]
        public async Task<IActionResult> GetPermissionByName(string name)
        {
            try
            {
                var permission = await _permissionService.GetPermissionByNameAsync(name);
                if (permission == null)
                {
                    return NotFound(new { success = false, message = "İzin bulunamadı." });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        permission.Id,
                        permission.Name,
                        permission.DisplayName,
                        permission.Description,
                        permission.Module,
                        permission.SortOrder,
                        permission.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/permissions/modules
        // Tüm modülleri listeler
        // =====================================================================
        /// <summary>
        /// Sistemdeki tüm modülleri getirir.
        /// </summary>
        /// <returns>Modül listesi</returns>
        [HttpGet("modules")]
        public async Task<IActionResult> GetModules()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionsAsync();
                var modules = permissions
                    .Select(p => p.Module)
                    .Distinct()
                    .OrderBy(m => m)
                    .Select(m => new
                    {
                        Name = m,
                        DisplayName = GetModuleDisplayName(m)
                    })
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = modules,
                    count = modules.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/permissions/by-module/{module}
        // Belirli bir modüldeki izinleri getirir
        // =====================================================================
        /// <summary>
        /// Belirtilen modüldeki tüm izinleri getirir.
        /// </summary>
        /// <param name="module">Modül adı (örn: "Products")</param>
        /// <returns>Modüldeki izinler</returns>
        [HttpGet("by-module/{module}")]
        public async Task<IActionResult> GetPermissionsByModule(string module)
        {
            try
            {
                var permissions = await _permissionService.GetPermissionsByModuleAsync(module);
                var list = permissions.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.DisplayName,
                    p.Description,
                    p.SortOrder,
                    p.IsActive
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = list,
                    module = module,
                    moduleDisplayName = GetModuleDisplayName(module),
                    count = list.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // PUT: api/admin/permissions/{id}/toggle-status
        // İznin aktif/pasif durumunu değiştirir
        // =====================================================================
        /// <summary>
        /// Belirtilen iznin aktif/pasif durumunu değiştirir.
        /// </summary>
        /// <param name="id">İzin ID'si</param>
        /// <returns>Güncellenmiş izin durumu</returns>
        [HttpPut("{id:int}/toggle-status")]
        public async Task<IActionResult> TogglePermissionStatus(int id)
        {
            try
            {
                var permission = await _permissionService.GetPermissionByIdAsync(id);
                if (permission == null)
                {
                    return NotFound(new { success = false, message = "İzin bulunamadı." });
                }

                // Durumu tersine çevir
                permission.IsActive = !permission.IsActive;
                await _permissionService.UpdatePermissionAsync(permission);

                // Audit log
                var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                int.TryParse(userIdStr, out var userId);
                await _auditLogService.WriteAsync(
                    userId,
                    permission.IsActive ? "PermissionActivated" : "PermissionDeactivated",
                    "Permission",
                    id.ToString(),
                    new { wasActive = !permission.IsActive },
                    new { isActive = permission.IsActive }
                );

                return Ok(new
                {
                    success = true,
                    message = $"İzin durumu güncellendi: {(permission.IsActive ? "Aktif" : "Pasif")}",
                    data = new
                    {
                        permission.Id,
                        permission.Name,
                        permission.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/permissions/user/{userId}
        // Belirli bir kullanıcının tüm izinlerini getirir
        // =====================================================================
        /// <summary>
        /// Belirtilen kullanıcının sahip olduğu tüm izinleri getirir.
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <returns>Kullanıcının izinleri</returns>
        [HttpGet("user/{userId:int}")]
        public async Task<IActionResult> GetUserPermissions(int userId)
        {
            try
            {
                // Kullanıcının izin isimlerini al
                var userPermissionNames = await _permissionService.GetUserPermissionsAsync(userId);
                var permissionNameSet = userPermissionNames.ToHashSet();

                // Tüm izinleri al ve kullanıcınınkilerle eşleştir
                var allPermissions = await _permissionService.GetAllPermissionsAsync();
                var userPermissions = allPermissions
                    .Where(p => permissionNameSet.Contains(p.Name))
                    .Select(p => new
                    {
                        p.Id,
                        p.Name,
                        p.DisplayName,
                        p.Description,
                        p.Module
                    })
                    .ToList();

                // Modüle göre grupla
                var grouped = userPermissions
                    .GroupBy(p => p.Module)
                    .Select(g => new
                    {
                        Module = g.Key,
                        ModuleDisplayName = GetModuleDisplayName(g.Key),
                        Permissions = g.ToList()
                    })
                    .OrderBy(g => g.Module)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    userId = userId,
                    totalPermissions = userPermissions.Count,
                    data = grouped
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/permissions/check/{userId}
        // Kullanıcının belirli bir izne sahip olup olmadığını kontrol eder
        // =====================================================================
        /// <summary>
        /// Kullanıcının belirli bir izne sahip olup olmadığını kontrol eder.
        /// </summary>
        /// <param name="userId">Kullanıcı ID'si</param>
        /// <param name="permission">Kontrol edilecek izin adı</param>
        /// <returns>İzin durumu</returns>
        [HttpGet("check/{userId:int}")]
        public async Task<IActionResult> CheckUserPermission(int userId, [FromQuery] string permission)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(permission))
                {
                    return BadRequest(new { success = false, message = "İzin adı gereklidir." });
                }

                var hasPermission = await _permissionService.UserHasPermissionAsync(userId, permission);

                return Ok(new
                {
                    success = true,
                    userId = userId,
                    permission = permission,
                    hasPermission = hasPermission
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/permissions/statistics
        // İzin istatistiklerini getirir
        // =====================================================================
        /// <summary>
        /// Sistemdeki izin istatistiklerini getirir.
        /// </summary>
        /// <returns>İstatistikler</returns>
        [HttpGet("statistics")]
        public async Task<IActionResult> GetStatistics()
        {
            try
            {
                var permissions = await _permissionService.GetAllPermissionsAsync();
                var permissionList = permissions.ToList();

                var stats = new
                {
                    TotalPermissions = permissionList.Count,
                    ActivePermissions = permissionList.Count(p => p.IsActive),
                    InactivePermissions = permissionList.Count(p => !p.IsActive),
                    TotalModules = permissionList.Select(p => p.Module).Distinct().Count(),
                    PermissionsPerModule = permissionList
                        .GroupBy(p => p.Module)
                        .Select(g => new
                        {
                            Module = g.Key,
                            DisplayName = GetModuleDisplayName(g.Key),
                            Count = g.Count()
                        })
                        .OrderByDescending(x => x.Count)
                        .ToList()
                };

                return Ok(new { success = true, data = stats });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // Yardımcı Metodlar
        // =====================================================================

        /// <summary>
        /// Modül adını Türkçe görüntüleme adına çevirir.
        /// </summary>
        private static string GetModuleDisplayName(string module)
        {
            return module switch
            {
                "Dashboard" => "Gösterge Paneli",
                "Products" => "Ürünler",
                "Categories" => "Kategoriler",
                "Orders" => "Siparişler",
                "Users" => "Kullanıcılar",
                "Roles" => "Roller",
                "Campaigns" => "Kampanyalar",
                "Coupons" => "Kuponlar",
                "Couriers" => "Kargolar",
                "Shipping" => "Teslimat",
                "Reports" => "Raporlar",
                "Banners" => "Bannerlar",
                "Brands" => "Markalar",
                "Settings" => "Ayarlar",
                "Logs" => "Loglar",
                _ => module
            };
        }
    }
}
