namespace AuraRental.Service.Utils;

public static class PhoneNumberNormalizer
{
    public static string NormalizeVietnamese(string input)
    {
        var digits = new string(input.Where(char.IsDigit).ToArray());
        if (digits.StartsWith('0'))
        {
            digits = $"84{digits[1..]}";
        }

        if (!digits.StartsWith("84", StringComparison.Ordinal) || digits.Length is < 11 or > 12)
        {
            throw new ArgumentException("Số điện thoại Việt Nam không hợp lệ.", nameof(input));
        }

        return digits;
    }
}
