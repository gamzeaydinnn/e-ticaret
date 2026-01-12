// =============================================================================
// AdminRolesController.cs - RBAC Rol Yönetimi Controller'ı
// =============================================================================
// Bu controller, sistemdeki rollerin ve rol-izin ilişkilerinin yönetimi için
// API endpoint'lerini sağlar. Roller modülü izinlerine göre erişim kontrol edilir.
// =============================================================================

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.Constants;
using ECommerce.API.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// Rol ve rol-izin ilişkilerinin yönetimi için API endpoint'leri.
    /// </summary>
    [ApiController]
    [Authorize(Roles = Roles.AdminLike)]
    [Route("api/admin/roles")]
    public class AdminRolesController : ControllerBase
    {
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IPermissionService _permissionService;
        private readonly IAuditLogService _auditLogService;

        public AdminRolesController(
            IRolePermissionService rolePermissionService,
            IPermissionService permissionService,
            IAuditLogService auditLogService)
        {
            _rolePermissionService = rolePermissionService;
            _permissionService = permissionService;
            _auditLogService = auditLogService;
        }

        // =====================================================================
        // GET: api/admin/roles
        // Tüm rolleri listeler
        // =====================================================================
        /// <summary>
        /// Sistemdeki tüm rolleri getirir.
        /// </summary>
        /// <returns>Rol listesi</returns>
        [HttpGet]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _rolePermissionService.GetAllRolesAsync();
                var list = roles.Select(r => new
                {
                    r.Id,
                    r.Name,
                    r.DisplayName,
                    r.Description,
                    IsSystemRole = IsSystemRole(r.Name),
                    CanEdit = CanEditRole(r.Name)
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = list,
                    count = list.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/roles/{roleId}
        // Belirli bir rolün detaylarını ve izinlerini getirir
        // =====================================================================
        /// <summary>
        /// Belirtilen rolün detaylarını ve sahip olduğu izinleri getirir.
        /// </summary>
        /// <param name="roleId">Rol ID'si</param>
        /// <returns>Rol detayları ve izinleri</returns>
        [HttpGet("{roleId:int}")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetRoleById(int roleId)
        {
            try
            {
                var role = await _rolePermissionService.GetRoleWithPermissionsAsync(roleId);
                if (role == null)
                {
                    return NotFound(new { success = false, message = "Rol bulunamadı." });
                }

                // İzinleri modüle göre grupla
                var groupedPermissions = role.Permissions
                    .GroupBy(p => p.Module)
                    .Select(g => new
                    {
                        Module = g.Key,
                        ModuleDisplayName = GetModuleDisplayName(g.Key),
                        Permissions = g.Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.DisplayName
                        }).ToList()
                    })
                    .OrderBy(g => g.Module)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        role.Id,
                        role.Name,
                        role.DisplayName,
                        role.Description,
                        IsSystemRole = IsSystemRole(role.Name),
                        CanEdit = CanEditRole(role.Name),
                        PermissionCount = role.Permissions.Count,
                        PermissionsByModule = groupedPermissions
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/roles/by-name/{roleName}
        // Rol adına göre detay getirir
        // =====================================================================
        /// <summary>
        /// Belirtilen isme sahip rolün detaylarını getirir.
        /// </summary>
        /// <param name="roleName">Rol adı</param>
        /// <returns>Rol detayları</returns>
        [HttpGet("by-name/{roleName}")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetRoleByName(string roleName)
        {
            try
            {
                var roles = await _rolePermissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => 
                    r.Name.Equals(roleName, StringComparison.OrdinalIgnoreCase));

                if (role == null)
                {
                    return NotFound(new { success = false, message = "Rol bulunamadı." });
                }

                // Detaylı bilgi için ID ile çağır
                return await GetRoleById(role.Id);
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/roles/{roleId}/permissions
        // Belirli bir rolün izinlerini getirir
        // =====================================================================
        /// <summary>
        /// Belirtilen rolün sahip olduğu izinleri getirir.
        /// </summary>
        /// <param name="roleId">Rol ID'si</param>
        /// <returns>Rol izinleri</returns>
        [HttpGet("{roleId:int}/permissions")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetRolePermissions(int roleId)
        {
            try
            {
                var permissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
                var list = permissions.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.DisplayName,
                    p.Description,
                    p.Module
                }).ToList();

                return Ok(new
                {
                    success = true,
                    roleId = roleId,
                    data = list,
                    count = list.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // PUT: api/admin/roles/{roleId}/permissions
        // Rolün izinlerini günceller (toplu atama)
        // =====================================================================
        /// <summary>
        /// Belirtilen rolün izinlerini toplu olarak günceller.
        /// Mevcut tüm izinler kaldırılır ve yeni izinler atanır.
        /// </summary>
        /// <param name="roleId">Rol ID'si</param>
        /// <param name="request">Yeni izin ID'leri</param>
        /// <returns>Güncelleme sonucu</returns>
        [HttpPut("{roleId:int}/permissions")]
        [HasPermission(Permissions.Roles.ManagePermissions)]
        public async Task<IActionResult> UpdateRolePermissions(int roleId, [FromBody] UpdateRolePermissionsRequest request)
        {
            try
            {
                // Rol var mı kontrol et
                var roles = await _rolePermissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                if (role == null)
                {
                    return NotFound(new { success = false, message = "Rol bulunamadı." });
                }

                // SuperAdmin rolünün izinleri değiştirilemez
                if (role.Name == Roles.SuperAdmin)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "SuperAdmin rolünün izinleri değiştirilemez." 
                    });
                }

                // Sadece SuperAdmin diğer yönetici rollerini değiştirebilir
                if (role.Name == Roles.Admin && !User.IsInRole(Roles.SuperAdmin))
                {
                    return Forbid();
                }

                var userId = GetCurrentUserId();

                // Mevcut izinleri al
                var currentPermissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
                var currentPermissionIds = currentPermissions.Select(p => p.Id).ToList();

                // Kaldırılacak izinler
                var permissionsToRemove = currentPermissionIds.Except(request.PermissionIds).ToList();
                // Eklenecek izinler
                var permissionsToAdd = request.PermissionIds.Except(currentPermissionIds).ToList();

                // İzinleri kaldır
                foreach (var permissionId in permissionsToRemove)
                {
                    await _rolePermissionService.RemovePermissionFromRoleAsync(roleId, permissionId);
                }

                // Yeni izinleri ekle
                foreach (var permissionId in permissionsToAdd)
                {
                    await _rolePermissionService.AssignPermissionToRoleAsync(roleId, permissionId, userId);
                }

                // Audit log
                await _auditLogService.WriteAsync(GetCurrentUserId(), "RolePermissionsUpdated", "Role", roleId.ToString().ToString(), null, new { message = $"Rol '{role.Name}' izinleri güncellendi. Eklenen: {permissionsToAdd.Count}, Kaldırılan: {permissionsToRemove.Count}"
                 });

                // Güncel izinleri döndür
                var updatedPermissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);

                return Ok(new
                {
                    success = true,
                    message = "Rol izinleri başarıyla güncellendi.",
                    data = new
                    {
                        roleId = roleId,
                        roleName = role.Name,
                        permissionsAdded = permissionsToAdd.Count,
                        permissionsRemoved = permissionsToRemove.Count,
                        totalPermissions = updatedPermissions.Count()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // POST: api/admin/roles/{roleId}/permissions/{permissionId}
        // Role tek bir izin ekler
        // =====================================================================
        /// <summary>
        /// Belirtilen role tek bir izin ekler.
        /// </summary>
        /// <param name="roleId">Rol ID'si</param>
        /// <param name="permissionId">İzin ID'si</param>
        /// <returns>Ekleme sonucu</returns>
        [HttpPost("{roleId:int}/permissions/{permissionId:int}")]
        [HasPermission(Permissions.Roles.ManagePermissions)]
        public async Task<IActionResult> AddPermissionToRole(int roleId, int permissionId)
        {
            try
            {
                // Rol kontrol
                var roles = await _rolePermissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                if (role == null)
                {
                    return NotFound(new { success = false, message = "Rol bulunamadı." });
                }

                // SuperAdmin rolüne izin eklenemez
                if (role.Name == Roles.SuperAdmin)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "SuperAdmin rolünün izinleri değiştirilemez." 
                    });
                }

                // İzin kontrol
                var permission = await _permissionService.GetPermissionByIdAsync(permissionId);
                if (permission == null)
                {
                    return NotFound(new { success = false, message = "İzin bulunamadı." });
                }

                var userId = GetCurrentUserId();
                await _rolePermissionService.AssignPermissionToRoleAsync(roleId, permissionId, userId);

                // Audit log
                await _auditLogService.WriteAsync(GetCurrentUserId(), "PermissionAddedToRole", "RolePermission", $"{roleId}-{permissionId}".ToString(), null, new { message = $"'{permission.Name}' izni '{role.Name}' rolüne eklendi."
                 });

                return Ok(new
                {
                    success = true,
                    message = $"'{permission.DisplayName}' izni '{role.DisplayName}' rolüne başarıyla eklendi."
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // DELETE: api/admin/roles/{roleId}/permissions/{permissionId}
        // Rolden tek bir izin kaldırır
        // =====================================================================
        /// <summary>
        /// Belirtilen rolden tek bir izni kaldırır.
        /// </summary>
        /// <param name="roleId">Rol ID'si</param>
        /// <param name="permissionId">İzin ID'si</param>
        /// <returns>Kaldırma sonucu</returns>
        [HttpDelete("{roleId:int}/permissions/{permissionId:int}")]
        [HasPermission(Permissions.Roles.ManagePermissions)]
        public async Task<IActionResult> RemovePermissionFromRole(int roleId, int permissionId)
        {
            try
            {
                // Rol kontrol
                var roles = await _rolePermissionService.GetAllRolesAsync();
                var role = roles.FirstOrDefault(r => r.Id == roleId);
                if (role == null)
                {
                    return NotFound(new { success = false, message = "Rol bulunamadı." });
                }

                // SuperAdmin rolünden izin kaldırılamaz
                if (role.Name == Roles.SuperAdmin)
                {
                    return BadRequest(new 
                    { 
                        success = false, 
                        message = "SuperAdmin rolünün izinleri değiştirilemez." 
                    });
                }

                // İzin kontrol
                var permission = await _permissionService.GetPermissionByIdAsync(permissionId);
                if (permission == null)
                {
                    return NotFound(new { success = false, message = "İzin bulunamadı." });
                }

                var userId = GetCurrentUserId();
                await _rolePermissionService.RemovePermissionFromRoleAsync(roleId, permissionId);

                // Audit log
                await _auditLogService.WriteAsync(GetCurrentUserId(), "PermissionRemovedFromRole", "RolePermission", $"{roleId}-{permissionId}".ToString(), null, new { message = $"'{permission.Name}' izni '{role.Name}' rolünden kaldırıldı."
                 });

                return Ok(new
                {
                    success = true,
                    message = $"'{permission.DisplayName}' izni '{role.DisplayName}' rolünden başarıyla kaldırıldı."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/roles/{roleId}/available-permissions
        // Role atanabilecek izinleri getirir (henüz atanmamış olanlar)
        // =====================================================================
        /// <summary>
        /// Belirtilen role atanabilecek (henüz atanmamış) izinleri getirir.
        /// </summary>
        /// <param name="roleId">Rol ID'si</param>
        /// <returns>Atanabilir izinler</returns>
        [HttpGet("{roleId:int}/available-permissions")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetAvailablePermissionsForRole(int roleId)
        {
            try
            {
                var allPermissions = await _permissionService.GetAllPermissionsAsync();
                var rolePermissions = await _rolePermissionService.GetRolePermissionsAsync(roleId);
                var rolePermissionIds = rolePermissions.Select(p => p.Id).ToHashSet();

                // Henüz atanmamış izinler
                var availablePermissions = allPermissions
                    .Where(p => !rolePermissionIds.Contains(p.Id) && p.IsActive)
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
                var grouped = availablePermissions
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
                    roleId = roleId,
                    data = grouped,
                    count = availablePermissions.Count
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/roles/comparison
        // İki veya daha fazla rolün izinlerini karşılaştırır
        // =====================================================================
        /// <summary>
        /// Belirtilen rollerin izinlerini karşılaştırır.
        /// </summary>
        /// <param name="roleIds">Karşılaştırılacak rol ID'leri (virgülle ayrılmış)</param>
        /// <returns>Karşılaştırma sonucu</returns>
        [HttpGet("comparison")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> CompareRoles([FromQuery] string roleIds)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(roleIds))
                {
                    return BadRequest(new { success = false, message = "En az iki rol ID'si gereklidir." });
                }

                var ids = roleIds.Split(',')
                    .Select(id => int.TryParse(id.Trim(), out var parsed) ? parsed : (int?)null)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();

                if (ids.Count < 2)
                {
                    return BadRequest(new { success = false, message = "En az iki rol ID'si gereklidir." });
                }

                var allPermissions = await _permissionService.GetAllPermissionsAsync();
                var comparison = new List<object>();

                foreach (var roleId in ids)
                {
                    var role = await _rolePermissionService.GetRoleWithPermissionsAsync(roleId);
                    if (role != null)
                    {
                        comparison.Add(new
                        {
                            role.Id,
                            role.Name,
                            role.DisplayName,
                            PermissionCount = role.Permissions.Count,
                            Permissions = role.Permissions.Select(p => p.Name).ToList()
                        });
                    }
                }

                // Ortak izinler
                var allRolePermissions = comparison
                    .Cast<dynamic>()
                    .Select(r => ((IEnumerable<string>)r.Permissions).ToHashSet())
                    .ToList();

                var commonPermissions = allRolePermissions.Count > 0
                    ? allRolePermissions.Aggregate((a, b) => a.Intersect(b).ToHashSet())
                    : new HashSet<string>();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        Roles = comparison,
                        CommonPermissions = commonPermissions.ToList(),
                        CommonPermissionCount = commonPermissions.Count,
                        TotalUniquePermissions = allRolePermissions
                            .SelectMany(p => p)
                            .Distinct()
                            .Count()
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // =====================================================================
        // GET: api/admin/roles/matrix
        // Tüm roller ve izinlerin matris görünümünü getirir
        // =====================================================================
        /// <summary>
        /// Tüm roller ve izinlerin matris görünümünü getirir.
        /// Frontend'de checkbox grid olarak gösterilebilir.
        /// </summary>
        /// <returns>Rol-izin matrisi</returns>
        [HttpGet("matrix")]
        [HasPermission(Permissions.Roles.View)]
        public async Task<IActionResult> GetRolePermissionMatrix()
        {
            try
            {
                var roles = await _rolePermissionService.GetAllRolesAsync();
                var allPermissions = await _permissionService.GetAllPermissionsAsync();
                var permissionList = allPermissions.ToList();

                var matrix = new List<object>();

                foreach (var role in roles)
                {
                    var roleWithPerms = await _rolePermissionService.GetRoleWithPermissionsAsync(role.Id);
                    var rolePermissionNames = roleWithPerms?.Permissions.Select(p => p.Name).ToHashSet() 
                        ?? new HashSet<string>();

                    matrix.Add(new
                    {
                        RoleId = role.Id,
                        RoleName = role.Name,
                        RoleDisplayName = role.DisplayName,
                        IsSystemRole = IsSystemRole(role.Name),
                        CanEdit = CanEditRole(role.Name),
                        Permissions = permissionList.Select(p => new
                        {
                            PermissionId = p.Id,
                            PermissionName = p.Name,
                            HasPermission = rolePermissionNames.Contains(p.Name)
                        }).ToList()
                    });
                }

                // Modüle göre gruplanmış izin listesi (header için)
                var permissionHeaders = permissionList
                    .GroupBy(p => p.Module)
                    .Select(g => new
                    {
                        Module = g.Key,
                        ModuleDisplayName = GetModuleDisplayName(g.Key),
                        Permissions = g.OrderBy(p => p.SortOrder).Select(p => new
                        {
                            p.Id,
                            p.Name,
                            p.DisplayName
                        }).ToList()
                    })
                    .OrderBy(g => g.Module)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        PermissionHeaders = permissionHeaders,
                        RoleMatrix = matrix
                    }
                });
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
        /// Mevcut kullanıcının ID'sini döndürür.
        /// </summary>
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        /// <summary>
        /// Rolün sistem rolü olup olmadığını kontrol eder.
        /// Sistem rolleri silinemez.
        /// </summary>
        private static bool IsSystemRole(string roleName)
        {
            return roleName == Roles.SuperAdmin 
                || roleName == Roles.Admin 
                || roleName == Roles.User;
        }

        /// <summary>
        /// Rolün izinlerinin düzenlenip düzenlenemeyeceğini kontrol eder.
        /// SuperAdmin rolü düzenlenemez.
        /// </summary>
        private static bool CanEditRole(string roleName)
        {
            return roleName != Roles.SuperAdmin;
        }

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

    // =========================================================================
    // Request/Response DTO'ları
    // =========================================================================

    /// <summary>
    /// Rol izinlerini güncelleme isteği.
    /// </summary>
    public class UpdateRolePermissionsRequest
    {
        /// <summary>
        /// Role atanacak izin ID'leri.
        /// Mevcut tüm izinler kaldırılır ve bu listedeki izinler atanır.
        /// </summary>
        public List<int> PermissionIds { get; set; } = new();
    }
}
