using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace AuraRental.Service.Infrastructure.Persistence;

internal sealed class EnumValueConverter<TEnum>()
    : ValueConverter<TEnum, string>(
        value => ToUpperSnakeCase(value),
        value => FromUpperSnakeCase(value))
    where TEnum : struct, Enum
{
    private static string ToUpperSnakeCase(TEnum value)
    {
        var text = value.ToString();
        var result = new StringBuilder(text.Length + 4);

        for (var index = 0; index < text.Length; index++)
        {
            var character = text[index];
            if (index > 0 && char.IsUpper(character))
            {
                result.Append('_');
            }

            result.Append(char.ToUpper(character, CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }

    private static TEnum FromUpperSnakeCase(string value) =>
        Enum.Parse<TEnum>(value.Replace("_", string.Empty, StringComparison.Ordinal), ignoreCase: true);
}
