using System.Threading.Tasks;

namespace DirectRide.Api.Services
{
    public interface IFileStorageService
    {
        Task<string> UploadProfilePhotoAsync(Guid userId, byte[] fileContent, string contentType, CancellationToken cancellationToken = default);
        Task DeleteProfilePhotoAsync(string key, CancellationToken cancellationToken = default);
        string GetProfilePhotoUrl(string key);
    }
}
