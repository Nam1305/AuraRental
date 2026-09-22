using AuraRental.Domain.Entities;

namespace AuraRental.Service.Interface.Service;

public interface IPasswordService
{
    string Hash(User user, string password);
    PasswordCheckResult Verify(User user, string passwordHash, string password);
}

public enum PasswordCheckResult
{
    Failed,
    Success,
    SuccessRehashNeeded
}
