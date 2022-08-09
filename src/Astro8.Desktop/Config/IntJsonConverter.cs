using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astro8;

public class IntJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString()!.AsSpan();

            if (!stringValue.Contains('_'))
            {
                return ParseInt(stringValue);
            }

            Span<char> value = stackalloc char[stringValue.Length];

            var offset = 0;
            foreach (var c in stringValue)
            {
                if (c != '_')
                {
                    value[offset++] = c;
                }
            }

            return ParseInt(value);
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        throw new JsonException();
    }

    private static int ParseInt(ReadOnlySpan<char> span)
    {
        if (span.Length > 2 && span[0] == '0' && (span[1] is 'X' or 'x'))
        {
            return int.Parse(span[2..], NumberStyles.HexNumber);
        }

        if (span.Length > 2 && span[0] == '0' && (span[1] is 'B' or 'b'))
        {
            return Convert.ToInt32(span[2..].ToString(), 2);
        }

        return int.Parse(span);
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}
