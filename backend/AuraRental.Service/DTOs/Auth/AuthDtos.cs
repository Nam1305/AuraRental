namespace AuraRental.Service.DTOs.Auth;

public sealed record LoginRequest(string Identifier, string Password);

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTimeOffset ExpiresAt);
