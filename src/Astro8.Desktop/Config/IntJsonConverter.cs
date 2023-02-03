using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astro8;

public class IntJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return ParseInt(ref reader);
    }

    public static int ParseInt(ref Utf8JsonReader reader)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var span = reader.GetString()!.AsSpan();
            return ParseInt(span);
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        throw new JsonException();
    }

    public static int ParseInt(ReadOnlySpan<char> span)
    {
        if (!span.Contains('_'))
        {
            return ParseIntCore(span);
        }

        Span<char> value = stackalloc char[span.Length];

        var offset = 0;
        foreach (var c in span)
        {
            if (c != '_')
            {
                value[offset++] = c;
            }
        }

        return ParseIntCore(value);
    }

    private static int ParseIntCore(ReadOnlySpan<char> span)
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
