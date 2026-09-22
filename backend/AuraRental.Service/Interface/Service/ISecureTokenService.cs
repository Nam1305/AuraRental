namespace AuraRental.Service.Interface.Service;

public sealed record CustomerFormCredential(string Token, string Otp, DateTimeOffset ExpiresAt);

public interface ISecureTokenService
{
    CustomerFormCredential GenerateCustomerFormCredential();
    string Hash(string value);
    bool Verify(string plainText, string expectedHash);
}
