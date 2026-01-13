using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using PastebinApp.Application.Interfaces;

namespace PastebinApp.Infrastructure.Storage;

public class MinIoBlobStorageService : IBlobStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinIoBlobStorageService> _logger;
    private readonly string _bucketName;

    public MinIoBlobStorageService(
        IMinioClient minioClient,
        IConfiguration configuration,
        ILogger<MinIoBlobStorageService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
        _bucketName = configuration["MinIO:BucketName"] ?? "pastebin-content";

        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    public async Task UploadContentAsync(
        string hash,
        string content,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = GetObjectName(hash);
            var contentBytes = Encoding.UTF8.GetBytes(content);

            using var stream = new MemoryStream(contentBytes);

            var putObjectArgs = new PutObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithStreamData(stream)
                .WithObjectSize(stream.Length)
                .WithContentType("text/plain; charset=utf-8");

            await _minioClient.PutObjectAsync(putObjectArgs, cancellationToken);

            _logger.LogInformation(
                "Content uploaded to MinIO: {ObjectName}, Size: {Size} bytes",
                objectName, contentBytes.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload content to MinIO: {Hash}", hash);
            throw;
        }
    }

    public async Task<string> GetContentAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = GetObjectName(hash);

            using var memoryStream = new MemoryStream();

            var getObjectArgs = new GetObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName)
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                });

            await _minioClient.GetObjectAsync(getObjectArgs, cancellationToken);

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream, Encoding.UTF8);
            var content = await reader.ReadToEndAsync(cancellationToken);

            _logger.LogDebug("Content retrieved from MinIO: {ObjectName}", objectName);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content from MinIO: {Hash}", hash);
            throw;
        }
    }

    public async Task DeleteContentAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = GetObjectName(hash);

            var removeObjectArgs = new RemoveObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            await _minioClient.RemoveObjectAsync(removeObjectArgs, cancellationToken);

            _logger.LogInformation("Content deleted from MinIO: {ObjectName}", objectName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete content from MinIO: {Hash}", hash);
            throw;
        }
    }

    public async Task<bool> ExistsAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = GetObjectName(hash);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<long> GetContentSizeAsync(
        string hash,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var objectName = GetObjectName(hash);

            var statObjectArgs = new StatObjectArgs()
                .WithBucket(_bucketName)
                .WithObject(objectName);

            var stat = await _minioClient.StatObjectAsync(statObjectArgs, cancellationToken);
            return stat.Size;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get content size from MinIO: {Hash}", hash);
            throw;
        }
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExistsArgs = new BucketExistsArgs()
                .WithBucket(_bucketName);

            bool exists = await _minioClient.BucketExistsAsync(bucketExistsArgs);

            if (!exists)
            {
                var makeBucketArgs = new MakeBucketArgs()
                    .WithBucket(_bucketName);

                await _minioClient.MakeBucketAsync(makeBucketArgs);
                _logger.LogInformation("Created MinIO bucket: {BucketName}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure bucket exists: {BucketName}", _bucketName);
            throw;
        }
    }

    private string GetObjectName(string hash) => $"pastes/{hash}.txt";
}