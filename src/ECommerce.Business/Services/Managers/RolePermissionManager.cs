using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Data.Context;
using ECommerce.Entities.Concrete;
using ECommerce.Core.Constants;

namespace ECommerce.Business.Services.Managers
{
    /// <summary>
    /// Rol-Permission atama işlemleri servisi implementasyonu.
    /// RBAC sisteminin yönetim katmanıdır.
    /// 
    /// Özellikler:
    /// - Transaction desteği ile atomik operasyonlar
    /// - Cache invalidation ile tutarlılık
    /// - Audit logging için CreatedByUserId takibi
    /// 
    /// Güvenlik Notları:
    /// - SuperAdmin rolü korunmalı (tüm izinler silinmemeli)
    /// - Kendi rolünü değiştirmek tehlikeli olabilir
    /// </summary>
    public class RolePermissionManager : IRolePermissionService
    {
        private readonly ECommerceDbContext _context;
        private readonly RoleManager<IdentityRole<int>> _roleManager;
        private readonly UserManager<User> _userManager;
        private readonly IMemoryCache _cache;
        private readonly ILogger<RolePermissionManager> _logger;
        private readonly PermissionManager? _permissionManager;

        // Cache key prefix'leri
        private const string ROLE_PERMISSIONS_CACHE_KEY = "role_permissions_";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        // Türkçe rol görüntü adları
        private static readonly Dictionary<string, string> RoleDisplayNames = new()
        {
            { Roles.SuperAdmin, "Süper Yönetici" },
            { Roles.Admin, "Yönetici" },
            { Roles.StoreManager, "Mağaza Yöneticisi" },
            { Roles.CustomerSupport, "Müşteri Hizmetleri" },
            { Roles.Logistics, "Lojistik" },
            { Roles.User, "Kullanıcı" },
            { Roles.Customer, "Müşteri" }
        };

        public RolePermissionManager(
            ECommerceDbContext context,
            RoleManager<IdentityRole<int>> roleManager,
            UserManager<User> userManager,
            IMemoryCache cache,
            ILogger<RolePermissionManager> logger,
            IPermissionService? permissionService = null)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _roleManager = roleManager ?? throw new ArgumentNullException(nameof(roleManager));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            
            // Circular dependency'yi önlemek için opsiyonel
            _permissionManager = permissionService as PermissionManager;
        }

        #region Query Operations

        /// <inheritdoc />
        public async Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId)
        {
            try
            {
                return await _context.RolePermissions
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == roleId)
                    .Join(_context.Permissions.Where(p => p.IsActive),
                          rp => rp.PermissionId,
                          p => p.Id,
                          (rp, p) => p)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.SortOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol izinleri alınırken hata: RoleId={RoleId}", roleId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetRolePermissionNamesAsync(int roleId)
        {
            // Cache'ten kontrol et
            var cacheKey = $"{ROLE_PERMISSIONS_CACHE_KEY}{roleId}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                var permissions = await _context.RolePermissions
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == roleId)
                    .Join(_context.Permissions.Where(p => p.IsActive),
                          rp => rp.PermissionId,
                          p => p.Id,
                          (rp, p) => p.Name)
                    .ToListAsync();

                _cache.Set(cacheKey, permissions, CacheDuration);
                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol izin isimleri alınırken hata: RoleId={RoleId}", roleId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Permission>> GetRolePermissionsByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return Enumerable.Empty<Permission>();

            try
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null)
                    return Enumerable.Empty<Permission>();

                return await GetRolePermissionsAsync(role.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol izinleri alınırken hata: RoleName={RoleName}", roleName);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> RoleHasPermissionAsync(int roleId, string permissionName)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                return false;

            try
            {
                var permissions = await GetRolePermissionNamesAsync(roleId);
                return permissions.Any(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol izin kontrolü yapılırken hata: RoleId={RoleId}", roleId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<RoleWithPermissionsDto>> GetAllRolesWithPermissionsAsync()
        {
            try
            {
                var roles = await _roleManager.Roles.ToListAsync();
                var allPermissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.SortOrder)
                    .ToListAsync();

                var rolePermissions = await _context.RolePermissions
                    .AsNoTracking()
                    .ToListAsync();

                var result = new List<RoleWithPermissionsDto>();

                foreach (var role in roles)
                {
                    // Rol'deki kullanıcı sayısını al
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                    var assignedPermissionIds = rolePermissions
                        .Where(rp => rp.RoleId == role.Id)
                        .Select(rp => rp.PermissionId)
                        .ToHashSet();

                    result.Add(new RoleWithPermissionsDto
                    {
                        Id = role.Id,
                        Name = role.Name!,
                        DisplayName = GetRoleDisplayName(role.Name!),
                        Description = GetRoleDescription(role.Name!),
                        UserCount = usersInRole.Count,
                        Permissions = allPermissions.Select(p => new PermissionDto
                        {
                            Id = p.Id,
                            Name = p.Name,
                            DisplayName = p.DisplayName,
                            Description = p.Description,
                            Module = p.Module,
                            IsAssigned = assignedPermissionIds.Contains(p.Id)
                        }).ToList()
                    });
                }

                return result.OrderBy(r => GetRoleSortOrder(r.Name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Roller ve izinleri alınırken hata");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<RoleWithPermissionsDto?> GetRoleWithPermissionsAsync(int roleId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role == null)
                    return null;

                // Rolün izinlerini al
                var rolePermissions = await _context.RolePermissions
                    .AsNoTracking()
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToListAsync();

                var permissions = await _context.Permissions
                    .AsNoTracking()
                    .Where(p => rolePermissions.Contains(p.Id) && p.IsActive)
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.SortOrder)
                    .ToListAsync();

                // Rol'deki kullanıcı sayısını al
                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);

                return new RoleWithPermissionsDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    DisplayName = GetRoleDisplayName(role.Name!),
                    Description = GetRoleDescription(role.Name!),
                    UserCount = usersInRole.Count,
                    Permissions = permissions.Select(p => new PermissionDto
                    {
                        Id = p.Id,
                        Name = p.Name,
                        DisplayName = p.DisplayName,
                        Description = p.Description,
                        Module = p.Module,
                        IsAssigned = true
                    }).ToList()
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol izinleri alınırken hata: RoleId={RoleId}", roleId);
                throw;
            }
        }

        #endregion

        #region Assignment Operations

        /// <inheritdoc />
        public async Task AssignPermissionToRoleAsync(int roleId, int permissionId, int? assignedByUserId = null)
        {
            try
            {
                // Zaten atanmış mı kontrol et
                var existing = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (existing != null)
                {
                    _logger.LogDebug("İzin zaten atanmış: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
                    return; // Idempotent - sessizce dön
                }

                var rolePermission = new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = assignedByUserId
                };

                _context.RolePermissions.Add(rolePermission);
                await _context.SaveChangesAsync();

                // Cache'i temizle
                InvalidateRolePermissionsCache(roleId);

                _logger.LogInformation("İzin role atandı: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin atama hatası: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemovePermissionFromRoleAsync(int roleId, int permissionId)
        {
            try
            {
                var existing = await _context.RolePermissions
                    .FirstOrDefaultAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);

                if (existing == null)
                {
                    _logger.LogDebug("Kaldırılacak izin bulunamadı: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
                    return; // Idempotent - sessizce dön
                }

                _context.RolePermissions.Remove(existing);
                await _context.SaveChangesAsync();

                // Cache'i temizle
                InvalidateRolePermissionsCache(roleId);

                _logger.LogInformation("İzin rolden kaldırıldı: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin kaldırma hatası: RoleId={RoleId}, PermissionId={PermissionId}", roleId, permissionId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task AssignPermissionsToRoleAsync(int roleId, IEnumerable<int> permissionIds, int? assignedByUserId = null)
        {
            if (permissionIds == null || !permissionIds.Any())
                return;

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Mevcut atamaları al
                var existingIds = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .Select(rp => rp.PermissionId)
                    .ToHashSetAsync();

                // Sadece yeni olanları ekle
                var newPermissions = permissionIds
                    .Where(id => !existingIds.Contains(id))
                    .Select(id => new RolePermission
                    {
                        RoleId = roleId,
                        PermissionId = id,
                        CreatedAt = DateTime.UtcNow,
                        CreatedByUserId = assignedByUserId
                    })
                    .ToList();

                if (newPermissions.Any())
                {
                    await _context.RolePermissions.AddRangeAsync(newPermissions);
                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                // Cache'i temizle
                InvalidateRolePermissionsCache(roleId);

                _logger.LogInformation("İzinler role atandı: RoleId={RoleId}, Count={Count}", roleId, newPermissions.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Toplu izin atama hatası: RoleId={RoleId}", roleId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdateRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, int? assignedByUserId = null)
        {
            // SuperAdmin rolü koruması
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role?.Name == Roles.SuperAdmin)
            {
                _logger.LogWarning("SuperAdmin rolünün izinleri değiştirilemez");
                throw new InvalidOperationException("SuperAdmin rolünün izinleri değiştirilemez. Bu rol her zaman tüm izinlere sahiptir.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Mevcut atamaları sil
                var existingAssignments = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingAssignments);

                // Yeni atamaları ekle
                var newAssignments = permissionIds.Select(id => new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = assignedByUserId
                }).ToList();

                await _context.RolePermissions.AddRangeAsync(newAssignments);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                // Cache'i temizle
                InvalidateRolePermissionsCache(roleId);
                InvalidateAllUserPermissionsCache();

                _logger.LogInformation("Rol izinleri güncellendi: RoleId={RoleId}, Count={Count}", roleId, newAssignments.Count);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Rol izinleri güncelleme hatası: RoleId={RoleId}", roleId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task RemoveAllPermissionsFromRoleAsync(int roleId)
        {
            // SuperAdmin rolü koruması
            var role = await _roleManager.FindByIdAsync(roleId.ToString());
            if (role?.Name == Roles.SuperAdmin || role?.Name == Roles.Admin)
            {
                _logger.LogWarning("Admin rollerinin tüm izinleri kaldırılamaz: {RoleName}", role.Name);
                throw new InvalidOperationException($"{role.Name} rolünün tüm izinleri kaldırılamaz.");
            }

            try
            {
                var existingAssignments = await _context.RolePermissions
                    .Where(rp => rp.RoleId == roleId)
                    .ToListAsync();

                _context.RolePermissions.RemoveRange(existingAssignments);
                await _context.SaveChangesAsync();

                // Cache'i temizle
                InvalidateRolePermissionsCache(roleId);
                InvalidateAllUserPermissionsCache();

                _logger.LogInformation("Tüm izinler rolden kaldırıldı: RoleId={RoleId}", roleId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Tüm izinleri kaldırma hatası: RoleId={RoleId}", roleId);
                throw;
            }
        }

        #endregion

        #region Role Management

        /// <inheritdoc />
        public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
        {
            try
            {
                var roles = await _roleManager.Roles.ToListAsync();
                var result = new List<RoleDto>();

                foreach (var role in roles)
                {
                    var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                    var permissionCount = await _context.RolePermissions
                        .CountAsync(rp => rp.RoleId == role.Id);

                    result.Add(new RoleDto
                    {
                        Id = role.Id,
                        Name = role.Name!,
                        DisplayName = GetRoleDisplayName(role.Name!),
                        Description = GetRoleDescription(role.Name!),
                        UserCount = usersInRole.Count,
                        PermissionCount = permissionCount
                    });
                }

                return result.OrderBy(r => GetRoleSortOrder(r.Name));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Roller alınırken hata");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<RoleDto?> GetRoleByIdAsync(int roleId)
        {
            try
            {
                var role = await _roleManager.FindByIdAsync(roleId.ToString());
                if (role == null)
                    return null;

                var usersInRole = await _userManager.GetUsersInRoleAsync(role.Name!);
                var permissionCount = await _context.RolePermissions
                    .CountAsync(rp => rp.RoleId == role.Id);

                return new RoleDto
                {
                    Id = role.Id,
                    Name = role.Name!,
                    DisplayName = GetRoleDisplayName(role.Name!),
                    Description = GetRoleDescription(role.Name!),
                    UserCount = usersInRole.Count,
                    PermissionCount = permissionCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol alınırken hata: RoleId={RoleId}", roleId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<RoleDto?> GetRoleByNameAsync(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
                return null;

            try
            {
                var role = await _roleManager.FindByNameAsync(roleName);
                if (role == null)
                    return null;

                return await GetRoleByIdAsync(role.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Rol alınırken hata: RoleName={RoleName}", roleName);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        private static string GetRoleDisplayName(string roleName)
        {
            return RoleDisplayNames.TryGetValue(roleName, out var displayName)
                ? displayName
                : roleName;
        }

        private static string? GetRoleDescription(string roleName)
        {
            return roleName switch
            {
                Roles.SuperAdmin => "Sistemin tam yetkili yöneticisi. Tüm ayarları değiştirebilir.",
                Roles.Admin => "Yönetici. Geriye dönük uyumluluk için korunuyor.",
                Roles.StoreManager => "Mağaza operasyonlarını yönetir. Ürün, kategori, kampanya yetkisi.",
                Roles.CustomerSupport => "Müşteri hizmetleri. Sipariş ve iade işlemleri.",
                Roles.Logistics => "Lojistik personeli. Kargo ve teslimat işlemleri.",
                Roles.User => "Standart kullanıcı/müşteri.",
                Roles.Customer => "Müşteri hesabı.",
                _ => null
            };
        }

        private static int GetRoleSortOrder(string roleName)
        {
            return roleName switch
            {
                Roles.SuperAdmin => 1,
                Roles.Admin => 2,
                Roles.StoreManager => 3,
                Roles.CustomerSupport => 4,
                Roles.Logistics => 5,
                Roles.User => 6,
                Roles.Customer => 7,
                _ => 99
            };
        }

        private void InvalidateRolePermissionsCache(int roleId)
        {
            var cacheKey = $"{ROLE_PERMISSIONS_CACHE_KEY}{roleId}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("Rol izin cache'i temizlendi: RoleId={RoleId}", roleId);
        }

        private void InvalidateAllUserPermissionsCache()
        {
            // PermissionManager varsa kullanıcı cache'lerini de temizle
            // Bu basit implementasyonda tüm kullanıcıların cache'ini temizleyemeyiz
            // Production'da Redis gibi distributed cache kullanılmalı
            _logger.LogDebug("Kullanıcı izin cache'leri güncellenmeli (rol değişikliği)");
        }

        #endregion
    }
}
