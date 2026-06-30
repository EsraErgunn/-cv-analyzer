using Minio;
using Minio.DataModel.Args;

namespace CVParserService.Services;

public class MinioDownloadService
{
    private readonly IMinioClient _minioClient;
    private const string BucketName = "cv-files";

    public MinioDownloadService(IMinioClient minioClient)
    {
        _minioClient = minioClient;
    }

    public async Task<MemoryStream> DownloadFileAsync(string objectName)
    {
        var memoryStream = new MemoryStream();

        await _minioClient.GetObjectAsync(new GetObjectArgs()
            .WithBucket(BucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream)));

        memoryStream.Position = 0;
        return memoryStream;
    }
}