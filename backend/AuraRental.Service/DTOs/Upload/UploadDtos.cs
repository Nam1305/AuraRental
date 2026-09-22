namespace AuraRental.Service.DTOs.Upload;

public sealed record CreateUploadUrlRequest(
    string Purpose,
    string FileName,
    string ContentType,
    long SizeBytes);

public sealed record UploadUrlDto(
    string ObjectPath,
    string UploadUrl,
    DateTimeOffset ExpiresAt);

public sealed record DownloadUrlDto(
    string DownloadUrl,
    DateTimeOffset ExpiresAt);
