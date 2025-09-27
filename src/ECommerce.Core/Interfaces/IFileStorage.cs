// ECommerce.Core/Interfaces/IFileStorage.cs
public interface IFileStorage
{
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);
    Task DeleteAsync(string fileUrl);
    Task<Stream> DownloadAsync(string fileUrl);
}
