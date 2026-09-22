using System.Security.Cryptography;
using System.Text;
using AuraRental.Service.Interface.Service;
using Microsoft.Extensions.Configuration;

namespace AuraRental.Service.Service;

public sealed class SecureTokenService : ISecureTokenService
{
    private readonly byte[] _pepper;

    public SecureTokenService(IConfiguration configuration)
    {
        var pepper = configuration["Security:TokenPepper"]
            ?? throw new InvalidOperationException("Security:TokenPepper is required.");
        if (Encoding.UTF8.GetByteCount(pepper) < 32)
        {
            throw new InvalidOperationException("Security:TokenPepper must contain at least 32 bytes.");
        }

        _pepper = Encoding.UTF8.GetBytes(pepper);
    }

    public CustomerFormCredential GenerateCustomerFormCredential()
    {
        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        var otp = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        return new CustomerFormCredential(token, otp, DateTimeOffset.UtcNow.AddHours(24));
    }

    public string Hash(string value)
    {
        using var hmac = new HMACSHA256(_pepper);
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(value)));
    }

    public bool Verify(string plainText, string expectedHash)
    {
        var actualBytes = Convert.FromHexString(Hash(plainText));
        byte[] expectedBytes;
        try
        {
            expectedBytes = Convert.FromHexString(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return actualBytes.Length == expectedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes);
    }
}
