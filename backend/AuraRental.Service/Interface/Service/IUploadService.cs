using AuraRental.Service.DTOs.Upload;

namespace AuraRental.Service.Interface.Service;

public interface IUploadService
{
    UploadUrlDto CreatePresignedUploadUrl(CreateUploadUrlRequest request);
    DownloadUrlDto CreatePresignedDownloadUrl(string objectPath);
}
