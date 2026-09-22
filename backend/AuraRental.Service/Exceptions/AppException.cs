using System.Net;

namespace AuraRental.Service.Exceptions;

public class AppException : Exception
{
    public AppException(
        string code,
        string message,
        HttpStatusCode statusCode,
        IReadOnlyDictionary<string, string>? fields = null)
        : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Fields = fields;
    }

    public string Code { get; }
    public HttpStatusCode StatusCode { get; }
    public IReadOnlyDictionary<string, string>? Fields { get; }
}

public sealed class NotFoundException(string code, string message)
    : AppException(code, message, HttpStatusCode.NotFound);

public sealed class ForbiddenException(string code, string message)
    : AppException(code, message, HttpStatusCode.Forbidden);

public sealed class ConflictException(string code, string message)
    : AppException(code, message, HttpStatusCode.Conflict);

public sealed class GoneException(string code, string message)
    : AppException(code, message, HttpStatusCode.Gone);

public sealed class ValidationException(
    string code,
    string message,
    IReadOnlyDictionary<string, string>? fields = null)
    : AppException(code, message, HttpStatusCode.BadRequest, fields);
