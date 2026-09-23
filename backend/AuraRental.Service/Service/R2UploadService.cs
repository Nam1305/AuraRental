using Amazon.S3;
using Amazon.S3.Model;
using System.Security.Cryptography;
using AuraRental.Service.DTOs.Upload;
using AuraRental.Service.Exceptions;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Options;
using Microsoft.Extensions.Options;

namespace AuraRental.Service.Service;

public sealed class R2UploadService(
    IOptions<R2Options> options) : IUploadService
{
    private const long MaxImageBytes = 10 * 1024 * 1024;
    private static readonly IReadOnlyDictionary<string, string> AllowedContentTypes =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/webp"] = ".webp"
        };

    public UploadUrlDto CreatePresignedUploadUrl(CreateUploadUrlRequest request)
    {
        if (!string.Equals(request.Purpose, "PRODUCT_IMAGE", StringComparison.Ordinal) &&
            !string.Equals(request.Purpose, "DAMAGE_EVIDENCE", StringComparison.Ordinal))
            throw new ValidationException("UPLOAD_PURPOSE_INVALID", "Mục đích tải ảnh không hợp lệ.");

        if (!AllowedContentTypes.TryGetValue(request.ContentType, out var extension))
            throw new ValidationException("UPLOAD_CONTENT_TYPE_INVALID", "Chỉ chấp nhận ảnh JPG, PNG hoặc WebP.");

        if (request.SizeBytes <= 0 || request.SizeBytes > MaxImageBytes)
            throw new ValidationException("UPLOAD_SIZE_INVALID", "Ảnh phải lớn hơn 0 và không quá 10 MB.");

        if (string.IsNullOrWhiteSpace(request.FileName))
            throw new ValidationException("UPLOAD_FILENAME_REQUIRED", "Tên tệp là bắt buộc.");

        var settings = options.Value;
        ValidateConfiguration(settings);
        var randomSuffix = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        var objectPath = $"{request.Purpose.ToLowerInvariant()}/{DateTime.UtcNow:yyyy/MM/dd}/{randomSuffix}{extension}";
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(settings.UploadUrlLifetimeSeconds);
        var presign = new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName,
            Key = objectPath,
            Verb = HttpVerb.PUT,
            ContentType = request.ContentType,
            Expires = expiresAt.UtcDateTime
        };

        using var s3 = CreateClient(settings);
        return new UploadUrlDto(objectPath, s3.GetPreSignedURL(presign), expiresAt);
    }

    public DownloadUrlDto CreatePresignedDownloadUrl(string objectPath)
    {
        var normalizedPath = objectPath.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPath) || normalizedPath.StartsWith('/') || normalizedPath.Contains("..", StringComparison.Ordinal))
            throw new ValidationException("OBJECT_PATH_INVALID", "Đường dẫn ảnh không hợp lệ.");

        var settings = options.Value;
        ValidateConfiguration(settings);
        var expiresAt = DateTimeOffset.UtcNow.AddSeconds(settings.UploadUrlLifetimeSeconds);
        using var s3 = CreateClient(settings);
        var downloadUrl = s3.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = settings.BucketName,
            Key = normalizedPath,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime
        });
        return new DownloadUrlDto(downloadUrl, expiresAt);
    }

    private static AmazonS3Client CreateClient(R2Options settings)
    {
        var endpoint = string.IsNullOrWhiteSpace(settings.Endpoint)
            ? $"https://{settings.AccountId}.r2.cloudflarestorage.com"
            : settings.Endpoint;
        return new AmazonS3Client(
            new Amazon.Runtime.BasicAWSCredentials(settings.AccessKeyId, settings.SecretAccessKey),
            new AmazonS3Config { ServiceURL = endpoint, ForcePathStyle = true, AuthenticationRegion = "auto" });
    }

    private static void ValidateConfiguration(R2Options settings)
    {
        if (string.IsNullOrWhiteSpace(settings.AccessKeyId) ||
            string.IsNullOrWhiteSpace(settings.SecretAccessKey) ||
            string.IsNullOrWhiteSpace(settings.BucketName) ||
            (string.IsNullOrWhiteSpace(settings.Endpoint) && string.IsNullOrWhiteSpace(settings.AccountId)))
            throw new InvalidOperationException("Cloudflare R2 chưa được cấu hình. Thiết lập R2__AccessKeyId, R2__SecretAccessKey, R2__BucketName và R2__Endpoint (hoặc R2__AccountId).");

        if (settings.UploadUrlLifetimeSeconds is < 60 or > 3600)
            throw new InvalidOperationException("R2__UploadUrlLifetimeSeconds phải nằm trong khoảng 60 đến 3600.");
    }
}
