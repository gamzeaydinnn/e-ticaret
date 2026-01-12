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
    /// Permission (İzin) yönetimi servisi implementasyonu.
    /// RBAC sisteminin temel servislerinden biridir.
    /// 
    /// Özellikler:
    /// - In-memory cache ile performans optimizasyonu
    /// - Kullanıcı izinlerinin rollerden toplanması
    /// - Thread-safe operasyonlar
    /// 
    /// Cache Stratejisi:
    /// - Kullanıcı izinleri 5 dakika cache'lenir
    /// - Rol/izin değişikliklerinde cache invalidate edilir
    /// </summary>
    public class PermissionManager : IPermissionService
    {
        private readonly ECommerceDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IMemoryCache _cache;
        private readonly ILogger<PermissionManager> _logger;

        // Cache key prefix'leri
        private const string USER_PERMISSIONS_CACHE_KEY = "user_permissions_";
        private const string ALL_PERMISSIONS_CACHE_KEY = "all_permissions";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        public PermissionManager(
            ECommerceDbContext context,
            UserManager<User> userManager,
            IMemoryCache cache,
            ILogger<PermissionManager> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region CRUD Operations

        /// <inheritdoc />
        public async Task<IEnumerable<Permission>> GetAllPermissionsAsync(bool includeInactive = false)
        {
            try
            {
                // Cache'ten kontrol et
                var cacheKey = $"{ALL_PERMISSIONS_CACHE_KEY}_{includeInactive}";
                if (_cache.TryGetValue(cacheKey, out IEnumerable<Permission>? cached) && cached != null)
                {
                    return cached;
                }

                var query = _context.Permissions.AsNoTracking();
                
                if (!includeInactive)
                {
                    query = query.Where(p => p.IsActive);
                }

                var permissions = await query
                    .OrderBy(p => p.Module)
                    .ThenBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .ToListAsync();

                // Cache'e ekle
                _cache.Set(cacheKey, permissions, CacheDuration);

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permissions alınırken hata oluştu");
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Permission>> GetPermissionsByModuleAsync(string module)
        {
            if (string.IsNullOrWhiteSpace(module))
                throw new ArgumentException("Modül adı boş olamaz", nameof(module));

            try
            {
                return await _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive && p.Module.ToLower() == module.ToLower())
                    .OrderBy(p => p.SortOrder)
                    .ThenBy(p => p.Name)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Modül izinleri alınırken hata: {Module}", module);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Permission?> GetPermissionByIdAsync(int id)
        {
            try
            {
                return await _context.Permissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission alınırken hata: ID={Id}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Permission?> GetPermissionByNameAsync(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;

            try
            {
                return await _context.Permissions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Name.ToLower() == name.ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission alınırken hata: Name={Name}", name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<Permission> CreatePermissionAsync(Permission permission)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            // Validasyon
            if (string.IsNullOrWhiteSpace(permission.Name))
                throw new ArgumentException("Permission adı boş olamaz");

            if (string.IsNullOrWhiteSpace(permission.DisplayName))
                throw new ArgumentException("Permission görüntü adı boş olamaz");

            // Aynı isimde permission var mı kontrol et
            var existing = await GetPermissionByNameAsync(permission.Name);
            if (existing != null)
                throw new InvalidOperationException($"'{permission.Name}' adında bir izin zaten mevcut");

            try
            {
                permission.CreatedAt = DateTime.UtcNow;
                permission.IsActive = true;

                _context.Permissions.Add(permission);
                await _context.SaveChangesAsync();

                // Cache'i temizle
                InvalidateAllPermissionsCache();

                _logger.LogInformation("Yeni permission oluşturuldu: {Name}", permission.Name);
                return permission;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission oluşturulurken hata: {Name}", permission.Name);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task UpdatePermissionAsync(Permission permission)
        {
            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            try
            {
                var existing = await _context.Permissions.FindAsync(permission.Id);
                if (existing == null)
                    throw new InvalidOperationException($"ID={permission.Id} olan permission bulunamadı");

                // Sadece izin verilen alanları güncelle
                existing.DisplayName = permission.DisplayName;
                existing.Description = permission.Description;
                existing.SortOrder = permission.SortOrder;
                existing.IsActive = permission.IsActive;
                existing.UpdatedAt = DateTime.UtcNow;

                // Name ve Module değiştirilmemeli (veri bütünlüğü için)

                await _context.SaveChangesAsync();

                // Cache'i temizle
                InvalidateAllPermissionsCache();

                _logger.LogInformation("Permission güncellendi: {Name}", existing.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission güncellenirken hata: ID={Id}", permission.Id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task DeletePermissionAsync(int id)
        {
            try
            {
                var permission = await _context.Permissions.FindAsync(id);
                if (permission == null)
                    throw new InvalidOperationException($"ID={id} olan permission bulunamadı");

                // Soft delete - IsActive = false
                permission.IsActive = false;
                permission.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Cache'i temizle
                InvalidateAllPermissionsCache();

                _logger.LogInformation("Permission silindi (soft delete): {Name}", permission.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Permission silinirken hata: ID={Id}", id);
                throw;
            }
        }

        #endregion

        #region User Permission Queries

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetUserPermissionsAsync(int userId)
        {
            // Cache'ten kontrol et
            var cacheKey = $"{USER_PERMISSIONS_CACHE_KEY}{userId}";
            if (_cache.TryGetValue(cacheKey, out IEnumerable<string>? cached) && cached != null)
            {
                return cached;
            }

            try
            {
                // 1. Kullanıcıyı bul
                var user = await _userManager.FindByIdAsync(userId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("Kullanıcı bulunamadı: ID={UserId}", userId);
                    return Enumerable.Empty<string>();
                }

                // 2. Kullanıcının rollerini al
                var userRoles = await _userManager.GetRolesAsync(user);
                if (!userRoles.Any())
                {
                    // Kullanıcının rolü yoksa boş dön
                    return Enumerable.Empty<string>();
                }

                // 3. SuperAdmin kontrolü - tüm izinleri ver
                if (userRoles.Contains(Roles.SuperAdmin) || userRoles.Contains(Roles.Admin))
                {
                    var allPermissions = await _context.Permissions
                        .AsNoTracking()
                        .Where(p => p.IsActive)
                        .Select(p => p.Name)
                        .ToListAsync();

                    _cache.Set(cacheKey, allPermissions, CacheDuration);
                    return allPermissions;
                }

                // 4. Rollerin ID'lerini al
                var roleIds = await _context.Roles
                    .Where(r => userRoles.Contains(r.Name!))
                    .Select(r => r.Id)
                    .ToListAsync();

                // 5. Rol-Permission ilişkisinden izinleri çek
                var permissions = await _context.RolePermissions
                    .AsNoTracking()
                    .Where(rp => roleIds.Contains(rp.RoleId))
                    .Join(_context.Permissions.Where(p => p.IsActive),
                          rp => rp.PermissionId,
                          p => p.Id,
                          (rp, p) => p.Name)
                    .Distinct()
                    .ToListAsync();

                // Cache'e ekle
                _cache.Set(cacheKey, permissions, CacheDuration);

                return permissions;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Kullanıcı izinleri alınırken hata: UserId={UserId}", userId);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UserHasPermissionAsync(int userId, string permissionName)
        {
            if (string.IsNullOrWhiteSpace(permissionName))
                return false;

            try
            {
                var userPermissions = await GetUserPermissionsAsync(userId);
                return userPermissions.Any(p => p.Equals(permissionName, StringComparison.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin kontrolü yapılırken hata: UserId={UserId}, Permission={Permission}", 
                    userId, permissionName);
                return false; // Hata durumunda güvenlik için false dön
            }
        }

        /// <inheritdoc />
        public async Task<bool> UserHasAnyPermissionAsync(int userId, params string[] permissionNames)
        {
            if (permissionNames == null || permissionNames.Length == 0)
                return false;

            try
            {
                var userPermissions = await GetUserPermissionsAsync(userId);
                return permissionNames.Any(required => 
                    userPermissions.Any(p => p.Equals(required, StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin kontrolü yapılırken hata: UserId={UserId}", userId);
                return false;
            }
        }

        /// <inheritdoc />
        public async Task<bool> UserHasAllPermissionsAsync(int userId, params string[] permissionNames)
        {
            if (permissionNames == null || permissionNames.Length == 0)
                return true; // Boş liste = tüm izinler var kabul edilir

            try
            {
                var userPermissions = await GetUserPermissionsAsync(userId);
                return permissionNames.All(required => 
                    userPermissions.Any(p => p.Equals(required, StringComparison.OrdinalIgnoreCase)));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "İzin kontrolü yapılırken hata: UserId={UserId}", userId);
                return false;
            }
        }

        #endregion

        #region Utility Methods

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GetAllModulesAsync()
        {
            try
            {
                return await _context.Permissions
                    .AsNoTracking()
                    .Where(p => p.IsActive)
                    .Select(p => p.Module)
                    .Distinct()
                    .OrderBy(m => m)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Modüller alınırken hata");
                throw;
            }
        }

        /// <inheritdoc />
        public bool IsValidPermissionName(string permissionName)
        {
            return Permissions.IsValidPermission(permissionName);
        }

        /// <summary>
        /// Belirli bir kullanıcının permission cache'ini temizler.
        /// Rol değişikliklerinde çağrılmalıdır.
        /// </summary>
        public void InvalidateUserPermissionsCache(int userId)
        {
            var cacheKey = $"{USER_PERMISSIONS_CACHE_KEY}{userId}";
            _cache.Remove(cacheKey);
            _logger.LogDebug("Kullanıcı izin cache'i temizlendi: UserId={UserId}", userId);
        }

        /// <summary>
        /// Tüm permission cache'ini temizler.
        /// Permission ekleme/güncelleme/silme işlemlerinde çağrılır.
        /// </summary>
        public void InvalidateAllPermissionsCache()
        {
            _cache.Remove($"{ALL_PERMISSIONS_CACHE_KEY}_true");
            _cache.Remove($"{ALL_PERMISSIONS_CACHE_KEY}_false");
            _logger.LogDebug("Tüm permission cache'i temizlendi");
        }

        #endregion
    }
}
