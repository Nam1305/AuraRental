using AuraRental.Service.DTOs.Common;
using AuraRental.Service.DTOs.Upload;
using AuraRental.Service.Interface.Service;
using AuraRental.WebAPI.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraRental.WebAPI.Controllers;

[Route("api/v1/uploads")]
[Authorize]
[RequireBranch]
public sealed class UploadsController(IUploadService uploadService) : ApiControllerBase
{
    [HttpPost("presign")]
    public ActionResult<ApiResponse<UploadUrlDto>> CreatePresignedUrl([FromBody] CreateUploadUrlRequest request) =>
        ResponseData(uploadService.CreatePresignedUploadUrl(request));

    [HttpGet("presign-read")]
    public ActionResult<ApiResponse<DownloadUrlDto>> CreatePresignedReadUrl([FromQuery] string objectPath) =>
        ResponseData(uploadService.CreatePresignedDownloadUrl(objectPath));
}
