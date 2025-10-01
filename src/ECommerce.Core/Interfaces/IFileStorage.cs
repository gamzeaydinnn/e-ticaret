// ECommerce.Core/Interfaces/IFileStorage.cs
namespace ECommerce.Core.Interfaces
{
    public interface IFileStorage
    {
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
        Task DeleteAsync(string fileUrl);
        Task<Stream> DownloadAsync(string fileUrl);
    }
}//resim upload (thumbnail üretim opsiyonu).
