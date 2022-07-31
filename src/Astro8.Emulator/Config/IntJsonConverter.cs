using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Astro8.Config;

public class IntJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString()!.AsSpan();

            if (stringValue.Length > 2 && stringValue[0] == '0' && (stringValue[1] is 'X' or 'x'))
            {
                return int.Parse(stringValue[2..], NumberStyles.HexNumber);
            }

            if (stringValue.Length > 2 && stringValue[0] == '0' && (stringValue[1] is 'B' or 'b'))
            {
                return Convert.ToInt32(stringValue[2..].ToString(), 2);
            }

            return int.Parse(stringValue);
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt32();
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}