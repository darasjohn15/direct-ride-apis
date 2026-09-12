using DirectRide.Api.Services;
using Amazon.S3;
using Amazon.S3.Model;

public class S3FileStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;

    public S3FileStorageService(IAmazonS3 s3Client, IConfiguration configuration)
    {
        _s3Client = s3Client;
        _bucketName = configuration["AWS:BucketName"] ?? throw new ArgumentNullException("AWS:BucketName");
    }

    public async Task<string> UploadProfilePhotoAsync(Guid userId, byte[] fileContent, string contentType, CancellationToken cancellationToken = default)
    {
        var key = $"profile-photos/{userId}/profile";

        using var stream = new MemoryStream(fileContent);
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = stream,
            ContentType = contentType,
        };

        await _s3Client.PutObjectAsync(request, cancellationToken);

        return key;
    }

    public async Task DeleteProfilePhotoAsync(string key, CancellationToken cancellationToken = default)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = _bucketName,
            Key = key
        };

        await _s3Client.DeleteObjectAsync(request, cancellationToken);
    }

    public string GetProfilePhotoUrl(string key)
    {
        var request = new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key = key,
            Expires = DateTime.UtcNow.AddHours(1)
        };

        return _s3Client.GetPreSignedURL(request);
    }
}
