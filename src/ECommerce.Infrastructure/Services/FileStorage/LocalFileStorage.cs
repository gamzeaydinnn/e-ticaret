using System;
using System.IO;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;

namespace ECommerce.Infrastructure.Services.FileStorage
{
    /// <summary>
    /// Local dosya sistemi üzerinde dosya yükleme/silme işlemlerini gerçekleştirir.
    /// uploads/ ana klasörü altında alt klasörler destekler (banners, products vb.)
    /// 
    /// Güvenlik Önlemleri:
    /// - Path traversal koruması (../ engellenir)
    /// - Dosya adları sanitize edilir
    /// - Alt klasörler otomatik oluşturulur
    /// </summary>
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        /// <summary>
        /// LocalFileStorage constructor
        /// </summary>
        /// <param name="rootPath">Uygulamanın kök dizini (ContentRootPath)</param>
        public LocalFileStorage(string rootPath)
        {
            // Ana uploads klasörü
            _rootPath = Path.Combine(rootPath, "uploads");
            
            // uploads klasörünü oluştur (yoksa)
            EnsureDirectoryExists(_rootPath);
            
            // Alt klasörleri de oluştur (banners, products, categories vb.)
            EnsureDirectoryExists(Path.Combine(_rootPath, "banners"));
            EnsureDirectoryExists(Path.Combine(_rootPath, "products"));
            EnsureDirectoryExists(Path.Combine(_rootPath, "categories"));
        }

        /// <summary>
        /// Dosyayı uploads klasörüne yükler
        /// Dosya adı format: {prefix}_{timestamp}_{guid}.{ext}
        /// </summary>
        /// <param name="fileStream">Yüklenecek dosyanın stream'i</param>
        /// <param name="fileName">Orijinal dosya adı (prefix olarak kullanılır)</param>
        /// <param name="contentType">MIME type</param>
        /// <returns>Dosyanın public URL'i (/uploads/banners/xxx.jpg)</returns>
        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            // Dosya uzantısını ve adını al
            var ext = Path.GetExtension(fileName)?.ToLowerInvariant() ?? ".jpg";
            var originalName = Path.GetFileNameWithoutExtension(fileName);
            
            // Dosya adını sanitize et (güvenlik için)
            var safeName = SanitizeFileName(originalName);
            if (string.IsNullOrWhiteSpace(safeName))
            {
                safeName = "file";
            }

            // Benzersiz dosya adı oluştur: {safename}_{timestamp}_{shortguid}.{ext}
            // Örnek: banner_20260112143025_abc123.jpg
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
            var shortGuid = Guid.NewGuid().ToString("N").Substring(0, 8);
            var uniqueFileName = $"{safeName}_{timestamp}_{shortGuid}{ext}";
            
            // Dosya tipine göre alt klasör belirle
            var subFolder = GetSubFolderByFileName(safeName);
            var targetDir = Path.Combine(_rootPath, subFolder);
            
            // Alt klasörün var olduğundan emin ol
            EnsureDirectoryExists(targetDir);
            
            // Dosyayı kaydet
            var filePath = Path.Combine(targetDir, uniqueFileName);
            using (var file = File.Create(filePath))
            {
                await fileStream.CopyToAsync(file);
            }
            
            // Public URL döndür
            return $"/uploads/{subFolder}/{uniqueFileName}";
        }

        /// <summary>
        /// Dosyayı siler
        /// </summary>
        /// <param name="fileUrl">Silinecek dosyanın URL'i (/uploads/banners/xxx.jpg)</param>
        public Task DeleteAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                return Task.CompletedTask;
            }

            // URL'den dosya yolunu çıkar
            // /uploads/banners/xxx.jpg -> banners/xxx.jpg
            var relativePath = fileUrl.TrimStart('/');
            if (relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(8); // "uploads/" kısmını kaldır
            }

            // Path traversal koruması
            if (relativePath.Contains("..") || relativePath.Contains("~"))
            {
                throw new ArgumentException("Geçersiz dosya yolu");
            }

            var filePath = Path.Combine(_rootPath, relativePath);
            
            // Güvenlik: Sadece uploads klasörü içindeki dosyaları sil
            var fullPath = Path.GetFullPath(filePath);
            var rootFullPath = Path.GetFullPath(_rootPath);
            if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Dosya uploads klasörü dışında");
            }

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
            
            return Task.CompletedTask;
        }

        /// <summary>
        /// Dosyayı indirir (stream olarak)
        /// </summary>
        /// <param name="fileUrl">Dosyanın URL'i (/uploads/banners/xxx.jpg)</param>
        /// <returns>Dosya stream'i</returns>
        public Task<Stream> DownloadAsync(string fileUrl)
        {
            if (string.IsNullOrWhiteSpace(fileUrl))
            {
                throw new ArgumentNullException(nameof(fileUrl), "Dosya URL'i boş olamaz");
            }

            // URL'den dosya yolunu çıkar
            var relativePath = fileUrl.TrimStart('/');
            if (relativePath.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                relativePath = relativePath.Substring(8);
            }

            // Path traversal koruması
            if (relativePath.Contains("..") || relativePath.Contains("~"))
            {
                throw new ArgumentException("Geçersiz dosya yolu");
            }

            var filePath = Path.Combine(_rootPath, relativePath);
            
            // Güvenlik kontrolü
            var fullPath = Path.GetFullPath(filePath);
            var rootFullPath = Path.GetFullPath(_rootPath);
            if (!fullPath.StartsWith(rootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Dosya uploads klasörü dışında");
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Dosya bulunamadı", filePath);
            }

            return Task.FromResult<Stream>(File.OpenRead(filePath));
        }

        /// <summary>
        /// Dosya adı prefix'ine göre alt klasör belirler
        /// </summary>
        /// <param name="fileName">Dosya adı prefix'i</param>
        /// <returns>Alt klasör adı (banners, products, categories veya genel)</returns>
        private static string GetSubFolderByFileName(string fileName)
        {
            var lowerName = fileName.ToLowerInvariant();
            
            if (lowerName.StartsWith("banner") || lowerName.StartsWith("poster") || lowerName.StartsWith("slider"))
            {
                return "banners";
            }
            if (lowerName.StartsWith("product") || lowerName.StartsWith("urun"))
            {
                return "products";
            }
            if (lowerName.StartsWith("category") || lowerName.StartsWith("kategori"))
            {
                return "categories";
            }
            
            // Varsayılan: banners (en çok kullanılan)
            return "banners";
        }

        /// <summary>
        /// Dosya adını güvenli hale getirir
        /// Tehlikeli karakterleri kaldırır
        /// </summary>
        /// <param name="fileName">Orijinal dosya adı</param>
        /// <returns>Sanitize edilmiş dosya adı</returns>
        private static string SanitizeFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
            {
                return "file";
            }

            // Geçersiz karakterleri kaldır
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(fileName
                .Where(c => !invalidChars.Contains(c) && c != '.' && c != ' ')
                .Take(50) // Maksimum 50 karakter
                .ToArray());

            // Sadece alfanumerik ve alt çizgi bırak
            sanitized = new string(sanitized
                .Where(c => char.IsLetterOrDigit(c) || c == '_' || c == '-')
                .ToArray());

            return string.IsNullOrWhiteSpace(sanitized) ? "file" : sanitized.ToLowerInvariant();
        }

        /// <summary>
        /// Klasörün var olduğundan emin olur, yoksa oluşturur
        /// </summary>
        /// <param name="path">Klasör yolu</param>
        private static void EnsureDirectoryExists(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
    }
}

