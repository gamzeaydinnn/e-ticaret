using System.Collections.Generic;
using System.Threading.Tasks;
using ECommerce.Entities.Concrete;

namespace ECommerce.Business.Services.Interfaces
{
    /// <summary>
    /// Permission (İzin) yönetimi için servis interface'i.
    /// RBAC sisteminin temel servislerinden biridir.
    /// 
    /// Sorumluluklar:
    /// - Permission CRUD işlemleri
    /// - Kullanıcı izinlerini sorgulama
    /// - İzin validasyonu
    /// 
    /// Neden ayrı bir servis:
    /// - Single Responsibility Principle (SRP)
    /// - Test edilebilirlik (Mock'lanabilir interface)
    /// - Dependency Injection ile gevşek bağlılık
    /// </summary>
    public interface IPermissionService
    {
        #region CRUD Operations

        /// <summary>
        /// Tüm izinleri getirir.
        /// Admin panelinde izin listesi için kullanılır.
        /// </summary>
        /// <param name="includeInactive">Pasif izinleri de dahil et</param>
        Task<IEnumerable<Permission>> GetAllPermissionsAsync(bool includeInactive = false);

        /// <summary>
        /// Belirli bir modüldeki izinleri getirir.
        /// Örnek: "Products" modülündeki tüm izinler
        /// </summary>
        /// <param name="module">Modül adı (Products, Orders, Users vb.)</param>
        Task<IEnumerable<Permission>> GetPermissionsByModuleAsync(string module);

        /// <summary>
        /// İzni ID ile getirir.
        /// </summary>
        Task<Permission?> GetPermissionByIdAsync(int id);

        /// <summary>
        /// İzni adına göre getirir.
        /// Örnek: "products.create"
        /// </summary>
        Task<Permission?> GetPermissionByNameAsync(string name);

        /// <summary>
        /// Yeni izin oluşturur.
        /// Genellikle sadece SuperAdmin kullanır.
        /// </summary>
        Task<Permission> CreatePermissionAsync(Permission permission);

        /// <summary>
        /// İzni günceller.
        /// </summary>
        Task UpdatePermissionAsync(Permission permission);

        /// <summary>
        /// İzni siler (soft delete).
        /// Mevcut atamalar etkilenmez, sadece yeni atama yapılamaz.
        /// </summary>
        Task DeletePermissionAsync(int id);

        #endregion

        #region User Permission Queries

        /// <summary>
        /// Kullanıcının sahip olduğu tüm izinleri getirir.
        /// Kullanıcının rollerinden izinler toplanır.
        /// 
        /// Performans notu: 
        /// Bu metod sıklıkla çağrılacağından cache kullanımı önerilir.
        /// </summary>
        /// <param name="userId">Kullanıcı ID</param>
        Task<IEnumerable<string>> GetUserPermissionsAsync(int userId);

        /// <summary>
        /// Kullanıcının belirli bir izne sahip olup olmadığını kontrol eder.
        /// Authorization middleware tarafından kullanılır.
        /// </summary>
        /// <param name="userId">Kullanıcı ID</param>
        /// <param name="permissionName">İzin adı (örn: "products.create")</param>
        Task<bool> UserHasPermissionAsync(int userId, string permissionName);

        /// <summary>
        /// Kullanıcının belirli izinlerden herhangi birine sahip olup olmadığını kontrol eder.
        /// OR mantığı ile çalışır.
        /// </summary>
        Task<bool> UserHasAnyPermissionAsync(int userId, params string[] permissionNames);

        /// <summary>
        /// Kullanıcının tüm belirtilen izinlere sahip olup olmadığını kontrol eder.
        /// AND mantığı ile çalışır.
        /// </summary>
        Task<bool> UserHasAllPermissionsAsync(int userId, params string[] permissionNames);

        #endregion

        #region Utility Methods

        /// <summary>
        /// Sistemdeki tüm modül isimlerini getirir.
        /// Admin panelinde filtreleme için kullanılır.
        /// </summary>
        Task<IEnumerable<string>> GetAllModulesAsync();

        /// <summary>
        /// İzin adının geçerli olup olmadığını kontrol eder.
        /// </summary>
        bool IsValidPermissionName(string permissionName);

        #endregion
    }
}
