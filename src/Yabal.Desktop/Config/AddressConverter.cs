using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Yabal.Devices;
using Yabal.Devices;

namespace Yabal;

public class AddressConverter : JsonConverter<Address>
{
    public override Address Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var stringValue = reader.GetString()!.AsSpan();

            if (!stringValue.Contains(':'))
            {
                return new Address(0, IntJsonConverter.ParseInt(stringValue));
            }

            var left = stringValue.Slice(0, stringValue.IndexOf(':'));
            var right = stringValue.Slice(stringValue.IndexOf(':') + 1);

            return new Address(
                IntJsonConverter.ParseInt(left),
                IntJsonConverter.ParseInt(right)
            );
        }

        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();

            var left = IntJsonConverter.ParseInt(ref reader);

            reader.Read();

            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return new Address(0, left);
            }

            var right = IntJsonConverter.ParseInt(ref reader);

            reader.Read();

            if (reader.TokenType != JsonTokenType.EndArray)
            {
                throw new JsonException();
            }

            return new Address(left, right);
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return new Address(0, reader.GetInt32());
        }

        throw new JsonException();
    }

    public override void Write(Utf8JsonWriter writer, Address value, JsonSerializerOptions options)
    {
        writer.WriteStringValue($"{value.Bank}:0x{value.Offset:X4}");
    }
}
