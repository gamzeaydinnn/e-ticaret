using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ECommerce.Business.Services.Interfaces;
using ECommerce.Core.DTOs;
using ECommerce.Core.Constants;
using ECommerce.Core.Interfaces;
using ECommerce.API.Data;
using ECommerce.API.Authorization;
using System.Security.Claims;

namespace ECommerce.API.Controllers.Admin
{
    /// <summary>
    /// Admin tarafından banner/poster yönetimi için controller.
    /// Tüm CRUD işlemleri + dosya yükleme desteği sağlar.
    /// Sadece Admin ve SuperAdmin rollerine açıktır.
    /// </summary>
    [ApiController]
    [Route("api/admin/banners")]
    [Authorize(Roles = Roles.AllStaff)]
    public class AdminBannersController : ControllerBase
    {
        private readonly IBannerService _bannerService;
        private readonly IFileStorage _fileStorage;
        private readonly IAuditLogService _auditLogService;
        private readonly ILogger<AdminBannersController> _logger;
        private readonly IServiceProvider _serviceProvider;

        // İzin verilen dosya türleri (güvenlik için whitelist yaklaşımı)
        // Görsel + kısa sessiz HD döngü videoları (mp4/webm)
        private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".mp4", ".webm" };
        private static readonly string[] AllowedMimeTypes =
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "video/mp4", "video/webm"
        };
        
        // Maksimum dosya boyutu: 50MB (kısa HD video için)
        private const long MaxFileSize = 50 * 1024 * 1024;

        private static bool IsAllowedMime(string? contentType, string extension)
        {
            var mime = (contentType ?? string.Empty).ToLowerInvariant();
            if (AllowedMimeTypes.Contains(mime))
                return true;

            // Bazı tarayıcılar/OS'ler video için octet-stream veya boş MIME gönderir
            if (string.IsNullOrWhiteSpace(mime) || mime == "application/octet-stream")
                return AllowedExtensions.Contains(extension);

            return false;
        }

        public AdminBannersController(
            IBannerService bannerService,
            IFileStorage fileStorage,
            IAuditLogService auditLogService,
            ILogger<AdminBannersController> logger,
            IServiceProvider serviceProvider)
        {
            _bannerService = bannerService;
            _fileStorage = fileStorage;
            _auditLogService = auditLogService;
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Tüm banner'ları listeler (admin için)
        /// Aktif/pasif tümünü döndürür, DisplayOrder'a göre sıralı
        /// </summary>
        [HttpGet]
        [HasPermission(Permissions.Banners.View)]
        public async Task<IActionResult> GetAll()
        {
            _logger.LogInformation("📋 Admin banner listesi isteniyor - UserId: {UserId}", GetAdminUserId());
            
            try
            {
                var banners = await _bannerService.GetAllAsync();
                _logger.LogInformation("✅ {Count} banner listelendi", banners.Count());
                return Ok(banners);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner listesi alınırken hata oluştu");
                return StatusCode(500, new { message = "Banner listesi alınırken bir hata oluştu" });
            }
        }

        /// <summary>
        /// ID'ye göre tek bir banner getirir
        /// </summary>
        [HttpGet("{id:int}")]
        [HasPermission(Permissions.Banners.View)]
        public async Task<IActionResult> GetById(int id)
        {
            _logger.LogInformation("🔍 Banner #{Id} detayı isteniyor", id);
            
            var banner = await _bannerService.GetByIdAsync(id);
            if (banner == null)
            {
                _logger.LogWarning("⚠️ Banner #{Id} bulunamadı", id);
                return NotFound(new { message = $"Banner #{id} bulunamadı" });
            }
            
            return Ok(banner);
        }

        /// <summary>
        /// Yeni banner oluşturur (JSON body ile, resim URL'i dışarıdan verilir)
        /// </summary>
        [HttpPost]
        [HasPermission(Permissions.Banners.Create)]
        public async Task<IActionResult> Create([FromBody] BannerDto dto)
        {
            _logger.LogInformation("➕ Yeni banner oluşturuluyor: {Title}", dto.Title);
            
            try
            {
                // Validasyon
                if (string.IsNullOrWhiteSpace(dto.Title))
                {
                    return BadRequest(new { message = "Banner başlığı zorunludur" });
                }
                if (string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    return BadRequest(new { message = "Banner görseli zorunludur" });
                }

                dto.CreatedAt = DateTime.UtcNow;
                await _bannerService.AddAsync(dto);
                
                // Audit log - yeni banner oluşturuldu
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "BannerCreated",
                    "Banner",
                    "0", // Yeni oluşturulan ID henüz bilinmiyor
                    null,
                    new { dto.Title, dto.Type, dto.IsActive, dto.DisplayOrder }
                );
                
                _logger.LogInformation("✅ Banner oluşturuldu: {Title}", dto.Title);
                return Ok(new { message = "Banner başarıyla oluşturuldu" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner oluşturulurken hata: {Title}", dto.Title);
                return StatusCode(500, new { message = "Banner oluşturulurken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Resim dosyası ile birlikte yeni banner oluşturur (multipart/form-data)
        /// Ana sayfa poster yönetimi için tercih edilen yöntem
        /// </summary>
        [HttpPost("upload")]
        [HasPermission(Permissions.Banners.Create)]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> CreateWithImage(
            [FromForm] string title,
            [FromForm] string? linkUrl,
            [FromForm] string type = "slider",
            [FromForm] bool isActive = true,
            [FromForm] int displayOrder = 0,
            IFormFile? image = null)
        {
            _logger.LogInformation("📤 Banner yükleme başlatılıyor: {Title}", title);
            
            try
            {
                // Validasyonlar
                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest(new { message = "Banner başlığı zorunludur" });
                }

                if (image == null || image.Length == 0)
                {
                    return BadRequest(new { message = "Görsel dosyası zorunludur" });
                }

                // Dosya boyutu kontrolü
                if (image.Length > MaxFileSize)
                {
                    return BadRequest(new { message = $"Dosya boyutu maksimum {MaxFileSize / (1024 * 1024)}MB olabilir" });
                }

                // Dosya türü kontrolü (extension)
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    return BadRequest(new { message = $"Desteklenen dosya türleri: {string.Join(", ", AllowedExtensions)}" });
                }

                // MIME type kontrolü (güvenlik için ek katman)
                if (!IsAllowedMime(image.ContentType, extension))
                {
                    return BadRequest(new { message = "Geçersiz dosya türü. Desteklenen: jpg, png, gif, webp, mp4, webm." });
                }

                // Dosyayı yükle
                string imageUrl;
                using (var stream = image.OpenReadStream())
                {
                    // LocalFileStorage kullanarak dosyayı kaydet
                    // Dosya adı: banner_{timestamp}_{guid}.{ext} formatında oluşturulur
                    var fileName = $"banner_{image.FileName}";
                    imageUrl = await _fileStorage.UploadAsync(stream, fileName, image.ContentType);
                }

                _logger.LogInformation("✅ Medya yüklendi: {ImageUrl}", imageUrl);

                // Banner'ı veritabanına kaydet
                var dto = new BannerDto
                {
                    Title = title.Trim(),
                    ImageUrl = imageUrl,
                    LinkUrl = linkUrl?.Trim() ?? string.Empty,
                    Type = type,
                    IsActive = isActive,
                    DisplayOrder = displayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                await _bannerService.AddAsync(dto);

                // Audit log
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "BannerUploaded",
                    "Banner",
                    "0",
                    null,
                    new { dto.Title, dto.ImageUrl, dto.Type, dto.IsActive, dto.DisplayOrder }
                );

                _logger.LogInformation("✅ Banner başarıyla oluşturuldu: {Title} - {ImageUrl}", title, imageUrl);
                
                return Ok(new { 
                    message = "Banner başarıyla yüklendi", 
                    imageUrl = imageUrl 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner yüklenirken hata: {Title}", title);
                return StatusCode(500, new { message = "Banner yüklenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Sadece medya dosyası yükler (banner oluşturmadan).
        /// Görsel (jpg/png/gif/webp) veya kısa sessiz video (mp4/webm) kabul eder.
        /// </summary>
        /// <param name="image">Yüklenecek dosya</param>
        /// <returns>Yüklenen dosyanın URL'ini döner</returns>
        [HttpPost("upload-image")]
        [HasPermission(Permissions.Banners.Create)]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> UploadImageOnly(IFormFile image)
        {
            _logger.LogInformation("📤 Banner medya yükleme başlatılıyor");
            
            try
            {
                // Dosya var mı kontrolü
                if (image == null || image.Length == 0)
                {
                    _logger.LogWarning("⚠️ Dosya seçilmedi");
                    return BadRequest(new { message = "Lütfen bir görsel veya video dosyası seçin." });
                }

                // Dosya boyutu kontrolü
                if (image.Length > MaxFileSize)
                {
                    _logger.LogWarning("⚠️ Dosya çok büyük: {Size}MB", image.Length / (1024 * 1024));
                    return BadRequest(new { message = $"Dosya boyutu maksimum {MaxFileSize / (1024 * 1024)}MB olabilir." });
                }

                // Dosya uzantısı kontrolü (whitelist yaklaşımı)
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("⚠️ Geçersiz dosya uzantısı: {Extension}", extension);
                    return BadRequest(new { message = $"Desteklenen dosya türleri: {string.Join(", ", AllowedExtensions)}" });
                }

                // MIME type kontrolü (güvenlik için ek katman)
                var mimeType = image.ContentType.ToLowerInvariant();
                if (!IsAllowedMime(image.ContentType, extension))
                {
                    _logger.LogWarning("⚠️ Geçersiz MIME type: {MimeType}", mimeType);
                    return BadRequest(new { message = "Geçersiz dosya türü. Desteklenen: jpg, png, gif, webp, mp4, webm." });
                }

                // Dosyayı LocalFileStorage üzerinden yükle
                // Dosya adı: banner_{timestamp}_{guid}.{ext} formatında oluşturulur
                string imageUrl;
                using (var stream = image.OpenReadStream())
                {
                    var fileName = $"banner_{image.FileName}";
                    imageUrl = await _fileStorage.UploadAsync(stream, fileName, image.ContentType);
                }

                _logger.LogInformation("✅ Banner medyası yüklendi: {ImageUrl}", imageUrl);

                // Audit log
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "BannerImageUploaded",
                    "Banner",
                    "0",
                    null,
                    new { imageUrl, originalFileName = image.FileName, fileSize = image.Length }
                );

                // Başarılı yanıt - yüklenen dosyanın URL'ini döndür
                return Ok(new { 
                    success = true,
                    imageUrl = imageUrl,
                    message = "Medya başarıyla yüklendi."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner medyası yüklenirken hata oluştu");
                return StatusCode(500, new { message = "Dosya yüklenirken bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        /// <summary>
        /// Mevcut banner'ı günceller (JSON body ile)
        /// </summary>
        [HttpPut("{id:int}")]
        [HasPermission(Permissions.Banners.Update)]
        public async Task<IActionResult> Update(int id, [FromBody] BannerDto dto)
        {
            _logger.LogInformation("✏️ Banner #{Id} güncelleniyor", id);
            
            try
            {
                var existingBanner = await _bannerService.GetByIdAsync(id);
                if (existingBanner == null)
                {
                    return NotFound(new { message = $"Banner #{id} bulunamadı" });
                }

                // ID'yi body'den değil URL'den al (güvenlik)
                dto.Id = id;
                dto.UpdatedAt = DateTime.UtcNow;
                
                await _bannerService.UpdateAsync(dto);

                // Audit log - güncelleme kaydı
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "BannerUpdated",
                    "Banner",
                    id.ToString(),
                    new { existingBanner.Title, existingBanner.ImageUrl, existingBanner.Type, existingBanner.IsActive },
                    new { dto.Title, dto.ImageUrl, dto.Type, dto.IsActive }
                );

                _logger.LogInformation("✅ Banner #{Id} güncellendi", id);
                return Ok(new { message = "Banner başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner güncellenirken hata: #{Id}", id);
                return StatusCode(500, new { message = "Banner güncellenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Banner'ı resim dosyası ile günceller (multipart/form-data)
        /// Eski resim silinir, yeni resim yüklenir
        /// </summary>
        [HttpPut("{id:int}/upload")]
        [HasPermission(Permissions.Banners.Update)]
        [RequestSizeLimit(MaxFileSize)]
        public async Task<IActionResult> UpdateWithImage(
            int id,
            [FromForm] string title,
            [FromForm] string? linkUrl,
            [FromForm] string type = "slider",
            [FromForm] bool isActive = true,
            [FromForm] int displayOrder = 0,
            IFormFile? image = null)
        {
            _logger.LogInformation("📤 Banner #{Id} resim ile güncelleniyor", id);
            
            try
            {
                var existingBanner = await _bannerService.GetByIdAsync(id);
                if (existingBanner == null)
                {
                    return NotFound(new { message = $"Banner #{id} bulunamadı" });
                }

                if (string.IsNullOrWhiteSpace(title))
                {
                    return BadRequest(new { message = "Banner başlığı zorunludur" });
                }

                string imageUrl = existingBanner.ImageUrl;

                // Yeni resim yüklendiyse
                if (image != null && image.Length > 0)
                {
                    // Dosya boyutu kontrolü
                    if (image.Length > MaxFileSize)
                    {
                        return BadRequest(new { message = $"Dosya boyutu maksimum {MaxFileSize / (1024 * 1024)}MB olabilir" });
                    }

                    // Dosya türü kontrolü
                    var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
                    if (!AllowedExtensions.Contains(extension))
                    {
                        return BadRequest(new { message = $"Desteklenen dosya türleri: {string.Join(", ", AllowedExtensions)}" });
                    }

                    if (!IsAllowedMime(image.ContentType, extension))
                    {
                        return BadRequest(new { message = "Geçersiz dosya türü. Desteklenen: jpg, png, gif, webp, mp4, webm." });
                    }

                    // Eski resmi sil (eğer uploads klasöründeyse)
                    if (!string.IsNullOrEmpty(existingBanner.ImageUrl) && 
                        existingBanner.ImageUrl.StartsWith("/uploads/"))
                    {
                        try
                        {
                            await _fileStorage.DeleteAsync(existingBanner.ImageUrl);
                            _logger.LogInformation("🗑️ Eski resim silindi: {OldImage}", existingBanner.ImageUrl);
                        }
                        catch (Exception deleteEx)
                        {
                            // Silme hatası kritik değil, log'la ve devam et
                            _logger.LogWarning(deleteEx, "⚠️ Eski resim silinemedi: {OldImage}", existingBanner.ImageUrl);
                        }
                    }

                    // Yeni resmi yükle
                    using (var stream = image.OpenReadStream())
                    {
                        var fileName = $"banner_{image.FileName}";
                        imageUrl = await _fileStorage.UploadAsync(stream, fileName, image.ContentType);
                    }
                    
                    _logger.LogInformation("✅ Yeni resim yüklendi: {ImageUrl}", imageUrl);
                }

                // Banner'ı güncelle
                var dto = new BannerDto
                {
                    Id = id,
                    Title = title.Trim(),
                    ImageUrl = imageUrl,
                    LinkUrl = linkUrl?.Trim() ?? string.Empty,
                    Type = type,
                    IsActive = isActive,
                    DisplayOrder = displayOrder,
                    UpdatedAt = DateTime.UtcNow
                };

                await _bannerService.UpdateAsync(dto);

                // Audit log
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "BannerUpdatedWithImage",
                    "Banner",
                    id.ToString(),
                    new { existingBanner.Title, existingBanner.ImageUrl },
                    new { dto.Title, dto.ImageUrl }
                );

                _logger.LogInformation("✅ Banner #{Id} resim ile güncellendi", id);
                
                return Ok(new { 
                    message = "Banner başarıyla güncellendi", 
                    imageUrl = imageUrl 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner resim ile güncellenirken hata: #{Id}", id);
                return StatusCode(500, new { message = "Banner güncellenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Banner'ı siler
        /// İlişkili resim dosyası da silinir
        /// </summary>
        [HttpDelete("{id:int}")]
        [HasPermission(Permissions.Banners.Delete)]
        public async Task<IActionResult> Delete(int id)
        {
            _logger.LogInformation("🗑️ Banner #{Id} siliniyor", id);
            
            try
            {
                var existingBanner = await _bannerService.GetByIdAsync(id);
                if (existingBanner == null)
                {
                    return NotFound(new { message = $"Banner #{id} bulunamadı" });
                }

                // İlişkili resmi sil
                if (!string.IsNullOrEmpty(existingBanner.ImageUrl) && 
                    existingBanner.ImageUrl.StartsWith("/uploads/"))
                {
                    try
                    {
                        await _fileStorage.DeleteAsync(existingBanner.ImageUrl);
                        _logger.LogInformation("🗑️ İlişkili resim silindi: {ImageUrl}", existingBanner.ImageUrl);
                    }
                    catch (Exception deleteEx)
                    {
                        _logger.LogWarning(deleteEx, "⚠️ Resim dosyası silinemedi: {ImageUrl}", existingBanner.ImageUrl);
                    }
                }

                // Banner'ı sil
                await _bannerService.DeleteAsync(id);

                // Audit log
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "BannerDeleted",
                    "Banner",
                    id.ToString(),
                    new { existingBanner.Title, existingBanner.ImageUrl, existingBanner.Type },
                    null
                );

                _logger.LogInformation("✅ Banner #{Id} silindi", id);
                return Ok(new { message = "Banner başarıyla silindi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner silinirken hata: #{Id}", id);
                return StatusCode(500, new { message = "Banner silinirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Banner sıralamasını toplu günceller (drag & drop desteği)
        /// </summary>
        [HttpPatch("reorder")]
        [HasPermission(Permissions.Banners.Update)]
        public async Task<IActionResult> Reorder([FromBody] List<BannerOrderDto> orders)
        {
            _logger.LogInformation("🔄 Banner sıralaması güncelleniyor - {Count} öğe", orders.Count);
            
            try
            {
                foreach (var order in orders)
                {
                    var banner = await _bannerService.GetByIdAsync(order.Id);
                    if (banner != null)
                    {
                        banner.DisplayOrder = order.DisplayOrder;
                        banner.UpdatedAt = DateTime.UtcNow;
                        await _bannerService.UpdateAsync(banner);
                    }
                }

                _logger.LogInformation("✅ Banner sıralaması güncellendi");
                return Ok(new { message = "Sıralama başarıyla güncellendi" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner sıralaması güncellenirken hata");
                return StatusCode(500, new { message = "Sıralama güncellenirken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Banner aktif/pasif durumunu değiştirir (toggle)
        /// </summary>
        [HttpPatch("{id:int}/toggle")]
        [HasPermission(Permissions.Banners.Update)]
        public async Task<IActionResult> ToggleActive(int id)
        {
            _logger.LogInformation("🔀 Banner #{Id} aktiflik durumu değiştiriliyor", id);
            
            try
            {
                var banner = await _bannerService.GetByIdAsync(id);
                if (banner == null)
                {
                    return NotFound(new { message = $"Banner #{id} bulunamadı" });
                }

                banner.IsActive = !banner.IsActive;
                banner.UpdatedAt = DateTime.UtcNow;
                await _bannerService.UpdateAsync(banner);

                _logger.LogInformation("✅ Banner #{Id} durumu değiştirildi: IsActive={IsActive}", id, banner.IsActive);
                
                return Ok(new { 
                    message = $"Banner {(banner.IsActive ? "aktif" : "pasif")} yapıldı",
                    isActive = banner.IsActive
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner durumu değiştirilirken hata: #{Id}", id);
                return StatusCode(500, new { message = "Durum değiştirilirken bir hata oluştu" });
            }
        }



        /// <summary>
        /// Tipe göre banner'ları filtreler
        /// Örn: slider, promo, banner
        /// </summary>
        [HttpGet("type/{type}")]
        [HasPermission(Permissions.Banners.View)]
        public async Task<IActionResult> GetByType(string type)
        {
            _logger.LogInformation("📋 {Type} tipindeki banner'lar listeleniyor", type);
            
            try
            {
                var allBanners = await _bannerService.GetAllAsync();
                var filtered = allBanners.Where(b => 
                    b.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
                
                return Ok(filtered);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner listesi alınırken hata - Tip: {Type}", type);
                return StatusCode(500, new { message = "Banner listesi alınırken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Tüm banner'ları silip varsayılan değerlere sıfırlar
        /// Admin panelinden "Varsayılana Sıfırla" butonu için
        /// DİKKAT: Bu işlem geri alınamaz, tüm özel banner'lar silinir
        /// </summary>
        [HttpPost("reset-to-default")]
        [HasPermission(Permissions.Banners.Delete)]
        public async Task<IActionResult> ResetToDefault()
        {
            _logger.LogWarning("🔄 Banner'lar varsayılana sıfırlanıyor - UserId: {UserId}", GetAdminUserId());
            
            try
            {
                // BannerSeeder'ın ResetToDefaultAsync metodunu çağır
                await BannerSeeder.ResetToDefaultAsync(_serviceProvider);
                
                // Audit log
                await _auditLogService.WriteAsync(
                    GetAdminUserId(),
                    "BannersResetToDefault",
                    "Banner",
                    "ALL",
                    null,
                    new { action = "Tüm banner'lar varsayılana sıfırlandı" }
                );

                _logger.LogInformation("✅ Banner'lar varsayılana sıfırlandı");
                return Ok(new { message = "Banner'lar varsayılan değerlere sıfırlandı (7 banner oluşturuldu)" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Banner'lar sıfırlanırken hata oluştu");
                return StatusCode(500, new { message = "Banner'lar sıfırlanırken bir hata oluştu" });
            }
        }

        /// <summary>
        /// Admin kullanıcı ID'sini JWT token'dan alır
        /// </summary>
        private int GetAdminUserId()
        {
            var userIdValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("sub")?.Value;
            return int.TryParse(userIdValue, out var adminId) ? adminId : 0;
        }
    }

    /// <summary>
    /// Banner sıralama güncellemesi için DTO
    /// </summary>
    public class BannerOrderDto
    {
        public int Id { get; set; }
        public int DisplayOrder { get; set; }
    }
}
