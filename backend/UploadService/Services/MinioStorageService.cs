using Minio;
using Minio.DataModel.Args;

namespace UploadService.Services;

public class MinioStorageService
{
    private readonly IMinioClient _minioClient;
    private const string BucketName = "cv-files";

    public MinioStorageService(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task EnsureBucketExistsAsync()
    {
        var exists = await _minioClient.BucketExistsAsync(
            new BucketExistsArgs().WithBucket(BucketName));

        if (!exists)
            await _minioClient.MakeBucketAsync(
                new MakeBucketArgs().WithBucket(BucketName));
    }

    public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
    {
        await EnsureBucketExistsAsync();

        var objectName = $"{Guid.NewGuid()}/{fileName}";

        await _minioClient.PutObjectAsync(new PutObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithStreamData(fileStream)
            .WithObjectSize(fileStream.Length)
            .WithContentType(contentType));

        return objectName;
    }
}