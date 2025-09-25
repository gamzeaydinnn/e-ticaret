using System;
using System.IO;
using System.Threading.Tasks;

namespace ECommerce.Infrastructure.Services.FileStorage
{
    public class LocalFileStorage
    {
        private readonly string _basePath;

        public LocalFileStorage(string basePath)
        {
            _basePath = basePath;

            if (!Directory.Exists(_basePath))
            {
                Directory.CreateDirectory(_basePath);
            }
        }

        /// <summary>
        /// Dosya kaydeder ve kaydedilen dosyanın yolunu döner.
        /// </summary>
        public async Task<string> SaveFileAsync(byte[] fileBytes, string fileName, string folder = "")
        {
            string folderPath = string.IsNullOrEmpty(folder) ? _basePath : Path.Combine(_basePath, folder);

            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

            string fullPath = Path.Combine(folderPath, fileName);

            await File.WriteAllBytesAsync(fullPath, fileBytes);

            return fullPath;
        }

        /// <summary>
        /// Dosyayı siler.
        /// </summary>
        public void DeleteFile(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
