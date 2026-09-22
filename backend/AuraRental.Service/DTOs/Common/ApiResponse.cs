using System.Text.Json.Serialization;

namespace AuraRental.Service.DTOs.Common;

public sealed record ApiMeta(
    string RequestId,
    string? NextCursor = null,
    bool? HasMore = null);

public sealed record ApiResponse<T>(
    [property: JsonPropertyName("data")] T Data,
    [property: JsonPropertyName("meta")] ApiMeta Meta);

public sealed record ApiErrorBody(
    string Code,
    string Message,
    IReadOnlyDictionary<string, string>? Fields,
    string RequestId);

public sealed record ApiErrorResponse(ApiErrorBody Error);
