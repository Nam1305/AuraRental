using AuraRental.Service.DTOs.Auth;

namespace AuraRental.Service.Interface.UseCase;

public interface IAuthUseCase
{
    Task<LoginResponse> Login(LoginRequest request, CancellationToken cancellationToken);
}
