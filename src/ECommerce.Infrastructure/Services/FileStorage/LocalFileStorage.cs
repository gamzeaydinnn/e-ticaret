using System;
using System.IO;
using System.Threading.Tasks;
using ECommerce.Core.Interfaces;


namespace ECommerce.Infrastructure.Services.FileStorage
{
    public class LocalFileStorage : IFileStorage
    {
        private readonly string _rootPath;

        public LocalFileStorage(string rootPath)
        {
            _rootPath = Path.Combine(rootPath, "uploads");
            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            var filePath = Path.Combine(_rootPath, fileName);
            using (var file = File.Create(filePath))
            {
                await fileStream.CopyToAsync(file);
            }
            return $"/uploads/{fileName}";
        }

        public Task DeleteAsync(string fileUrl)
        {
            var filePath = Path.Combine(_rootPath, Path.GetFileName(fileUrl));
            if (File.Exists(filePath))
                File.Delete(filePath);
            return Task.CompletedTask;
        }

        public Task<Stream> DownloadAsync(string fileUrl)
        {
            var filePath = Path.Combine(_rootPath, Path.GetFileName(fileUrl));
            return Task.FromResult<Stream>(File.OpenRead(filePath));
        }
    }
}//		○ FileStorage (Local, S3, Azure Blob) implementasyonu.


