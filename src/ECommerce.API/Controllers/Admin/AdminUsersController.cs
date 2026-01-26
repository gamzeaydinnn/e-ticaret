using Microsoft.AspNetCore.Authorization;
using ECommerce.Core.Constants;
using Microsoft.AspNetCore.Mvc;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs.User;
using ECommerce.Entities.Concrete;
using Microsoft.AspNetCore.Identity;
using ECommerce.API.Authorization;
using System.Threading.Tasks;
using System.Linq;
using System.Security.Claims;
using ECommerce.Business.Services.Managers;
using Microsoft.Extensions.Logging;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// Admin kullanıcı yönetim API'si.
    /// RBAC tabanlı yetkilendirme kullanır.
    /// 
    /// KRİTİK: Kullanıcı rolü güncellenirken hem User.Role property'si
    /// hem de AspNetUserRoles tablosu güncellenir. Bu tutarlılık
    /// [Authorize(Roles=...)] ve PermissionService için gereklidir.
    /// </summary>
    [ApiController]
    [Authorize(Roles = Roles.AdminLike)]
    [Route("api/admin/users")]
    public class AdminUsersController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuditLogService _auditLogService;
        private readonly UserManager<User> _userManager;  // Identity UserManager
        private readonly PermissionManager? _permissionManager;  // Cache invalidation için
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            IUserService userService, 
            IAuditLogService auditLogService,
            UserManager<User> userManager,
            ILogger<AdminUsersController> logger,
            IPermissionService? permissionService = null)  // Opsiyonel - DI'dan gelir
        {
            _userService = userService;
            _auditLogService = auditLogService;
            _userManager = userManager;
            _logger = logger;
            // PermissionManager'a cast et (cache invalidation için)
            _permissionManager = permissionService as PermissionManager;
        }

        [HttpGet]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _userService.GetAllAsync();
                
                // ============================================================================
                // Madde 7 Düzeltmesi: Kullanıcı listesinde eksik bilgiler eklendi
                // - CreatedAt: Oluşturulma tarihi
                // - IsActive: Aktif/Pasif durumu
                // - LastLoginAt: Son giriş tarihi
                // ============================================================================
                var userList = users.Select(u => new
                {
                    u.Id,
                    u.Email,
                    u.FirstName,
                    u.LastName,
                    FullName = $"{u.FirstName} {u.LastName}",
                    u.IsActive,
                    u.CreatedAt,
                    u.LastLoginAt,  // Son giriş tarihi
                    u.Role
                }).ToList();

                return Ok(new { success = true, data = userList, count = userList.Count });
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{id}")]
        [HasPermission(Permissions.Users.View)]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost]
        [HasPermission(Permissions.Users.Create)]
        public async Task<IActionResult> CreateUser([FromBody] UserCreateDto dto)
        {
            // Rol validasyonu
            var targetRole = string.IsNullOrWhiteSpace(dto.Role) ? Roles.User : dto.Role;
            if (!IsAllowedRole(targetRole))
            {
                return BadRequest(new { success = false, message = "Geçersiz rol değeri." });
            }

            // SuperAdmin rolü sadece SuperAdmin tarafından atanabilir
            if (targetRole == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
            {
                return Forbid();
            }

            // Email benzersizlik kontrolü - Identity UserManager kullan
            var existingUser = await _userManager.FindByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                return BadRequest(new { success = false, message = "Bu email adresi zaten kullanılıyor." });
            }

            // User entity oluşturma
            // Identity UserManager tüm gerekli alanları (SecurityStamp, NormalizedEmail vb.) otomatik doldurur
            var user = new User
            {
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                FullName = $"{dto.FirstName} {dto.LastName}",
                UserName = dto.Email,
                Address = dto.Address,
                City = dto.City,
                Role = targetRole,
                IsActive = true,
                EmailConfirmed = true,  // Admin oluşturduğu için email doğrulanmış kabul edilir
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                // ============================================================================
                // Identity UserManager ile kullanıcı oluştur
                // Şifre hashleme ve tüm Identity alanları (SecurityStamp, NormalizedEmail vb.) otomatik
                // ============================================================================
                var result = await _userManager.CreateAsync(user, dto.Password);
                
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = $"Kullanıcı oluşturulamadı: {errors}" });
                }

                // ============================================================================
                // KRİTİK: Identity UserRoles tablosuna rol ataması
                // Bu atama yapılmazsa:
                // 1. User.IsInRole() çalışmaz
                // 2. [Authorize(Roles = "...")] attribute'ları çalışmaz
                // 3. RolePermission tablosu üzerinden izin kontrolü yapılamaz
                // 
                // User.Role string alanı + Identity UserRoles tablosu birlikte kullanılır
                // Tutarlılık için her ikisi de set edilmeli
                // ============================================================================
                var roleResult = await _userManager.AddToRoleAsync(user, targetRole);
                if (!roleResult.Succeeded)
                {
                    // Kullanıcı oluşturuldu ama rol atanamadı - kritik hata
                    // Kullanıcıyı silmek yerine loglayıp devam ediyoruz (kullanıcı var ama rolsüz)
                    var roleErrors = string.Join(", ", roleResult.Errors.Select(e => e.Description));
                    // Loglama yapılabilir: logger.LogWarning("Rol atanamadı: {Errors}", roleErrors);
                    
                    // Yine de başarılı dön ama uyarı mesajı ekle
                    return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
                    {
                        success = true,
                        warning = $"Kullanıcı oluşturuldu ancak rol atamasında sorun oluştu: {roleErrors}",
                        message = "Kullanıcı oluşturuldu. Rol manuel olarak atanmalı.",
                        data = new
                        {
                            user.Id,
                            user.Email,
                            user.FirstName,
                            user.LastName,
                            user.FullName,
                            user.Role,
                            user.IsActive,
                            user.CreatedAt
                        }
                    });
                }
                
                // Başarılı yanıt - hem kullanıcı hem rol ataması tamamlandı
                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
                {
                    success = true,
                    message = "Kullanıcı başarıyla oluşturuldu ve rol atandı.",
                    data = new
                    {
                        user.Id,
                        user.Email,
                        user.FirstName,
                        user.LastName,
                        user.FullName,
                        user.Role,
                        user.IsActive,
                        user.CreatedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Kullanıcı oluşturulurken hata: {ex.Message}" });
            }
        }

        [HttpPut("{id}")]
        [HasPermission(Permissions.Users.Update)]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var oldSnapshot = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.IsActive
            };

            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Role))
            {
                if (!IsAllowedRole(dto.Role))
                {
                    return BadRequest(new { success = false, message = "Geçersiz rol değeri." });
                }

                if (dto.Role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
                {
                    return Forbid();
                }

                // ============================================================================
                // KRİTİK: Identity UserRoles tablosunu da güncelle
                // Sadece user.Role property'sini değiştirmek yetmez!
                // [Authorize(Roles = "...")] ve PermissionService için Identity rolü gerekli
                // ============================================================================
                var oldRole = user.Role;
                
                // Mevcut Identity rollerini kaldır
                var currentIdentityRoles = await _userManager.GetRolesAsync(user);
                if (currentIdentityRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentIdentityRoles);
                }
                
                // Yeni Identity rolünü ata
                var identityRoleResult = await _userManager.AddToRoleAsync(user, dto.Role);
                if (!identityRoleResult.Succeeded)
                {
                    var roleErrors = string.Join(", ", identityRoleResult.Errors.Select(e => e.Description));
                    _logger.LogWarning("Identity rol ataması başarısız: {Errors}", roleErrors);
                    // Devam et - en azından user.Role güncellensin
                }
                
                // User entity'sindeki Role alanını da güncelle
                user.Role = dto.Role;
                
                // Permission cache'ini temizle (rol değişti)
                _permissionManager?.InvalidateUserPermissionsCache(id);
            }
            // şifre güncelleme opsiyonel olarak eklenebilir

            await _userService.UpdateAsync(user);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "UserUpdated",
                "User",
                id.ToString(),
                oldSnapshot,
                new
                {
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.Role,
                    user.IsActive
                });
            return NoContent();
        }

        [HttpDelete("{id}")]
        [HasPermission(Permissions.Users.Delete)]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var oldSnapshot = new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.Role,
                user.IsActive
            };

            await _userService.DeleteAsync(user);
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "UserDisabled",
                "User",
                id.ToString(),
                oldSnapshot,
                new
                {
                    user.IsActive
                });
            return NoContent();
        }

        // ============================================================================
        // Kullanıcı Rol Güncelleme
        // DÜZELTME: [Authorize(Roles = Roles.AdminLike)] yerine [HasPermission] kullanıldı
        // Bu sayede sadece users.roles iznine sahip kullanıcılar rol değiştirebilir
        // ============================================================================
        [HttpPut("{id}/role")]
        [HasPermission(Permissions.Users.ManageRoles)]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UserRoleUpdateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Role))
            {
                return BadRequest(new { success = false, message = "Rol zorunludur." });
            }

            var user = await _userService.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            if (!IsAllowedRole(dto.Role))
            {
                return BadRequest(new { success = false, message = "Geçersiz rol değeri." });
            }

            if (dto.Role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
            {
                return Forbid();
            }

            var oldRole = user.Role;
            
            // ============================================================================
            // KRİTİK: Identity UserRoles tablosunu güncelle
            // 1. Önce mevcut rolü kaldır (varsa)
            // 2. Sonra yeni rolü ata
            // Bu sayede [Authorize(Roles = "...")] ve User.IsInRole() doğru çalışır
            // ============================================================================
            
            // Mevcut rolleri al ve kaldır
            var currentRoles = await _userManager.GetRolesAsync(user);
            if (currentRoles.Any())
            {
                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = $"Mevcut rol kaldırılamadı: {errors}" });
                }
            }
            
            // Yeni rolü ata
            var addResult = await _userManager.AddToRoleAsync(user, dto.Role);
            if (!addResult.Succeeded)
            {
                var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                return BadRequest(new { success = false, message = $"Yeni rol atanamadı: {errors}" });
            }
            
            // User entity'sindeki Role alanını da güncelle (tutarlılık için)
            user.Role = dto.Role;
            await _userService.UpdateAsync(user);
            
            // ============================================================================
            // KRİTİK: Kullanıcının izin cache'ini temizle
            // Rol değişikliği sonrası kullanıcının yeni izinleri hemen geçerli olsun
            // ============================================================================
            _permissionManager?.InvalidateUserPermissionsCache(id);
            
            // Audit log
            await _auditLogService.WriteAsync(
                GetAdminUserId(),
                "UserRoleUpdated",
                "User",
                id.ToString(),
                new { OldRole = oldRole },
                new { NewRole = user.Role });
                
            return Ok(new { success = true, id = user.Id, role = user.Role });
        }

        // ============================================================================
        // Madde 8: Admin Panelinden Şifre Güncelleme
        // Admin, herhangi bir kullanıcının şifresini güncelleyebilir
        // SuperAdmin şifresini sadece SuperAdmin güncelleyebilir
        // GÜVENLİK: Şifre değişikliğinde kullanıcının SecurityStamp'i güncellenir
        // Bu sayede mevcut token'lar geçersiz olur
        // ============================================================================
        [HttpPut("{id}/password")]
        [HasPermission(Permissions.Users.Update)]
        public async Task<IActionResult> UpdateUserPassword(int id, [FromBody] AdminPasswordUpdateDto dto)
        {
            // Validasyon
            if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                return BadRequest(new { success = false, message = "Yeni şifre zorunludur." });
            }

            // Şifre minimum uzunluk kontrolü
            if (dto.NewPassword.Length < 6)
            {
                return BadRequest(new { success = false, message = "Şifre en az 6 karakter olmalıdır." });
            }

            // Kullanıcıyı bul
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null)
            {
                return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });
            }

            // SuperAdmin şifresini sadece SuperAdmin güncelleyebilir
            if (user.Role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
            {
                return Forbid();
            }

            try
            {
                // Mevcut şifreyi kaldır
                var removeResult = await _userManager.RemovePasswordAsync(user);
                if (!removeResult.Succeeded)
                {
                    var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = $"Şifre kaldırılamadı: {errors}" });
                }

                // Yeni şifreyi ekle
                var addResult = await _userManager.AddPasswordAsync(user, dto.NewPassword);
                if (!addResult.Succeeded)
                {
                    var errors = string.Join(", ", addResult.Errors.Select(e => e.Description));
                    return BadRequest(new { success = false, message = $"Yeni şifre atanamadı: {errors}" });
                }

                // ============================================================================
                // GÜVENLİK: SecurityStamp'i güncelle
                // Bu işlem kullanıcının mevcut tüm token'larını geçersiz kılar
                // Kullanıcı bir sonraki istekte 401 alır ve yeniden giriş yapması gerekir
                // ============================================================================
                await _userManager.UpdateSecurityStampAsync(user);

                // Audit log
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "UserPasswordUpdated",
                    "User",
                    id.ToString(),
                    new { UserId = id, Email = user.Email },
                    new { PasswordChanged = true, ChangedAt = DateTime.UtcNow, SessionsInvalidated = true });

                return Ok(new { 
                    success = true, 
                    message = "Şifre başarıyla güncellendi. Kullanıcının mevcut oturumları sonlandırıldı.",
                    userId = user.Id,
                    email = user.Email
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = $"Şifre güncellenirken hata: {ex.Message}" });
            }
        }

        /// <summary>
        /// Sistemde tanımlı geçerli roller listesi.
        /// Tüm admin paneli rolleri + müşteri rolleri
        /// NOT: Courier rolü burada yok çünkü kurye ekleme Kurye Paneli'nden yapılır
        /// </summary>
        private static readonly HashSet<string> AllowedRoles = new(StringComparer.OrdinalIgnoreCase)
        {
            Roles.SuperAdmin,
            Roles.Admin,           // Geriye dönük uyumluluk
            Roles.StoreManager,
            Roles.CustomerSupport,
            Roles.Logistics,
            Roles.StoreAttendant,  // Market Görevlisi
            Roles.Dispatcher,      // Sevkiyat Görevlisi
            Roles.User,
            Roles.Customer          // User ile aynı, semantik
        };

        private static bool IsAllowedRole(string? role) =>
            !string.IsNullOrWhiteSpace(role) && AllowedRoles.Contains(role);

        private int GetAdminUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var adminId) ? adminId : 0;
        }
    }
}
