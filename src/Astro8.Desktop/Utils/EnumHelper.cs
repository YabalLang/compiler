using System.CommandLine.Parsing;
using System.Reflection;

namespace Astro8.Utils;

public static class EnumHelper
{
    public static IEnumerable<T> GetAttributes<T>(this Enum enumVal) where T : Attribute
    {
        var type = enumVal.GetType();
        var memInfo = type.GetMember(enumVal.ToString());
        return memInfo[0].GetCustomAttributes<T>(false);
    }

    public static List<T>? ParseEnum<T>(ArgumentResult result) where T : struct, Enum
    {
        if (result.Tokens.Count == 0)
        {
            return null;
        }

        var names = new Dictionary<string, T>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in Enum.GetValues<T>())
        {
            names[value.ToString()] = value;

            foreach (var alias in value.GetAttributes<AliasAttribute>().SelectMany(i => i.Aliases))
            {
                names[alias] = value;
            }
        }

        var values = new List<T>();
        var splitChars = new[] {',', ' '};

        foreach (var token in result.Tokens)
        {
            T outputFormat;

            var tokenValue = token.Value;

            if (tokenValue.IndexOfAny(splitChars) != -1)
            {
                foreach (var value in tokenValue.Split(splitChars))
                {
                    if (names.TryGetValue(value, out outputFormat))
                    {
                        values.Add(outputFormat);
                    }
                    else
                    {
                        result.ErrorMessage = $"Unknown value '{value}'";
                        return null;
                    }
                }

                continue;
            }

            if (names.TryGetValue(tokenValue, out outputFormat))
            {
                values.Add(outputFormat);
                continue;
            }

            foreach (var c in tokenValue)
            {
                if (names.TryGetValue(c.ToString(), out outputFormat))
                {
                    values.Add(outputFormat);
                }
                else
                {
                    result.ErrorMessage = $"Unknown value '{tokenValue}'";
                    return null;
                }
            }
        }

        return values;
    }
}
