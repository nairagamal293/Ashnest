namespace Ashnest.Services
{
    public interface IImageService
    {
        Task<byte[]> ConvertImageToByteArrayAsync(IFormFile imageFile);
        string GetImageMimeType(string fileName);
        bool IsValidImage(IFormFile imageFile);
    }
}
