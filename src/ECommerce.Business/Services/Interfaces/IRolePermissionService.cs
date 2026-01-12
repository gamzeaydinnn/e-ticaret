using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Rol-Permission atama işlemleri için servis interface'i.
    /// Rollere izin atama/kaldırma ve rol izinlerini sorgulama işlemlerini yönetir.
    /// 
    /// Sorumluluklar:
    /// - Rol-Permission atamaları
    /// - Rol izinlerini sorgulama
    /// - Toplu izin atama/kaldırma
    /// 
    /// Kullanım Senaryoları:
    /// - Admin panelinde rol yönetimi sayfası
    /// - Yeni rol oluşturma ve izin atama
    /// - Mevcut rolün izinlerini güncelleme
    /// </summary>
    public interface IRolePermissionService
    {
        #region Query Operations

        /// <summary>
        /// Belirli bir rolün sahip olduğu tüm izinleri getirir.
        /// </summary>
        /// <param name="roleId">ASP.NET Identity Role ID</param>
        Task<IEnumerable<Permission>> GetRolePermissionsAsync(int roleId);

        /// <summary>
        /// Belirli bir rolün sahip olduğu izin isimlerini getirir.
        /// Cache-friendly versiyon.
        /// </summary>
        Task<IEnumerable<string>> GetRolePermissionNamesAsync(int roleId);

        /// <summary>
        /// Rol adına göre izinleri getirir.
        /// </summary>
        /// <param name="roleName">Rol adı (SuperAdmin, StoreManager vb.)</param>
        Task<IEnumerable<Permission>> GetRolePermissionsByNameAsync(string roleName);

        /// <summary>
        /// Belirli bir rolün belirli bir izne sahip olup olmadığını kontrol eder.
        /// </summary>
        Task<bool> RoleHasPermissionAsync(int roleId, string permissionName);

        /// <summary>
        /// Tüm rolleri izinleriyle birlikte getirir.
        /// Admin panelinde rol-izin matris görünümü için.
        /// </summary>
        Task<IEnumerable<RoleWithPermissionsDto>> GetAllRolesWithPermissionsAsync();

        /// <summary>
        /// Belirli bir rolü izinleriyle birlikte getirir.
        /// </summary>
        /// <param name="roleId">Rol ID</param>
        /// <returns>Rol ve izinleri</returns>
        Task<RoleWithPermissionsDto?> GetRoleWithPermissionsAsync(int roleId);

        #endregion

        #region Assignment Operations

        /// <summary>
        /// Bir role izin atar.
        /// Aynı atama zaten varsa sessizce atlar (idempotent).
        /// </summary>
        /// <param name="roleId">Rol ID</param>
        /// <param name="permissionId">İzin ID</param>
        /// <param name="assignedByUserId">Atamayı yapan kullanıcı ID (audit için)</param>
        Task AssignPermissionToRoleAsync(int roleId, int permissionId, int? assignedByUserId = null);

        /// <summary>
        /// Bir rolden izni kaldırır.
        /// İzin atanmamışsa sessizce atlar (idempotent).
        /// </summary>
        Task RemovePermissionFromRoleAsync(int roleId, int permissionId);

        /// <summary>
        /// Bir role birden fazla izin atar.
        /// Transaction içinde çalışır - ya hepsi ya hiçbiri.
        /// </summary>
        Task AssignPermissionsToRoleAsync(int roleId, IEnumerable<int> permissionIds, int? assignedByUserId = null);

        /// <summary>
        /// Bir rolün izinlerini tamamen günceller.
        /// Mevcut izinler silinir, yeni liste atanır.
        /// </summary>
        Task UpdateRolePermissionsAsync(int roleId, IEnumerable<int> permissionIds, int? assignedByUserId = null);

        /// <summary>
        /// Bir rolün tüm izinlerini kaldırır.
        /// Tehlikeli operasyon - SuperAdmin için disabled olabilir.
        /// </summary>
        Task RemoveAllPermissionsFromRoleAsync(int roleId);

        #endregion

        #region Role Management

        /// <summary>
        /// Tüm sistem rollerini getirir.
        /// </summary>
        Task<IEnumerable<RoleDto>> GetAllRolesAsync();

        /// <summary>
        /// Rol ID'sine göre rol bilgisini getirir.
        /// </summary>
        Task<RoleDto?> GetRoleByIdAsync(int roleId);

        /// <summary>
        /// Rol adına göre rol bilgisini getirir.
        /// </summary>
        Task<RoleDto?> GetRoleByNameAsync(string roleName);

        #endregion
    }

    #region DTO Classes

    /// <summary>
    /// Rol bilgisi DTO.
    /// Frontend'e gönderilecek basitleştirilmiş rol bilgisi.
    /// </summary>
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserCount { get; set; }
        public int PermissionCount { get; set; }
    }

    /// <summary>
    /// Rol ve izinleri birlikte içeren DTO.
    /// Rol-izin matris görünümü için.
    /// </summary>
    public class RoleWithPermissionsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int UserCount { get; set; }
        public List<PermissionDto> Permissions { get; set; } = new();
    }

    /// <summary>
    /// İzin bilgisi DTO.
    /// Frontend'e gönderilecek basitleştirilmiş izin bilgisi.
    /// </summary>
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Module { get; set; } = string.Empty;
        public bool IsAssigned { get; set; }
    }

    #endregion
}
