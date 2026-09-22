using AuraRental.Service.DTOs.Auth;
using AuraRental.Service.Interface.Persistence;
using AuraRental.Service.Interface.Service;
using AuraRental.Service.Interface.UseCase;

namespace AuraRental.Service.UseCase;

public sealed class AuthUseCase(
    IUserRepository userRepository,
    IPasswordService passwordService,
    IAccessTokenService accessTokenService,
    IUnitOfWork unitOfWork) : IAuthUseCase
{
    public async Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var identifier = NormalizeIdentifier(request.Identifier);
        ValidatePassword(request.Password);

        var user = await userRepository.GetByLoginIdentifier(identifier, true, cancellationToken);
        if (user is null)
        {
            throw InvalidCredentials();
        }

        var passwordCheck = passwordService.Verify(user, user.PasswordHash, request.Password);
        if (passwordCheck == PasswordCheckResult.Failed)
        {
            throw InvalidCredentials();
        }

        if (passwordCheck == PasswordCheckResult.SuccessRehashNeeded)
        {
            user.PasswordHash = passwordService.Hash(user, request.Password);
            await unitOfWork.SaveChanges(cancellationToken);
        }

        var token = accessTokenService.Create(user);
        return new LoginResponse(token.Value, "Bearer", token.ExpiresAt);
    }

    private static string NormalizeIdentifier(string identifier)
    {
        var normalized = identifier?.Trim().ToLowerInvariant() ?? string.Empty;
        if (normalized.Length is < 3 or > 254)
        {
            throw InvalidCredentials();
        }

        return normalized;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length > 128)
        {
            throw InvalidCredentials();
        }
    }

    private static UnauthorizedAccessException InvalidCredentials() =>
        new("Email/username hoặc mật khẩu không đúng.");
}
