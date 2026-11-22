using System.ComponentModel;
using System.Reflection;

namespace Lexora.Shared.Helpers;

public static class EnumHelper
{
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        var trimmed = value?.Trim();
        var name = Enum.GetNames<TEnum>().FirstOrDefault(candidate => string.Equals(candidate, trimmed, StringComparison.OrdinalIgnoreCase));

        result = name is null ? default : Enum.Parse<TEnum>(name);

        return name is not null;
    }

    public static IReadOnlyList<TEnum> Values<TEnum>()
        where TEnum : struct, Enum => Enum.GetValues<TEnum>();

    public static string Description<TEnum>(TEnum value)
        where TEnum : struct, Enum
    {
        var name = value.ToString();
        var field = typeof(TEnum).GetField(name, BindingFlags.Public | BindingFlags.Static);

        return field?.GetCustomAttribute<DescriptionAttribute>()?.Description ?? name;
    }
}
