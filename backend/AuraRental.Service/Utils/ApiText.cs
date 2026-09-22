using System.Globalization;
using System.Text;

namespace AuraRental.Service.Utils;

public static class ApiText
{
    public static string EnumValue<TEnum>(TEnum value) where TEnum : struct, Enum
    {
        var source = value.ToString();
        var result = new StringBuilder(source.Length + 4);

        for (var index = 0; index < source.Length; index++)
        {
            var character = source[index];
            if (index > 0 && char.IsUpper(character))
            {
                result.Append('_');
            }

            result.Append(char.ToUpper(character, CultureInfo.InvariantCulture));
        }

        return result.ToString();
    }

    public static string PackageLabel(string packageCode) => packageCode.ToUpperInvariant() switch
    {
        "12H" => "12 giờ",
        "1D" => "1 ngày",
        "2D" => "2 ngày",
        "3D" => "3 ngày",
        _ => packageCode
    };
}
