using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Service;

public interface IAccessTokenService
{
    AccessToken Create(User user);
}

public sealed record AccessToken(string Value, DateTimeOffset ExpiresAt);
