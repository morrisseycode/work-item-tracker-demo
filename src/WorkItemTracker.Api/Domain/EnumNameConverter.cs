using System.Text.Json;
using System.Text.Json.Serialization;

namespace WorkItemTracker.Api.Domain;

// Accepts only a single defined name (case-insensitive). The built-in string enum
// converter also accepts numbers and comma-separated names such as "Low, High".
public class EnumNameConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    public override TEnum Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var text = reader.TokenType == JsonTokenType.String ? reader.GetString() : null;

        return EnumNames.TryParse<TEnum>(text, out var value) ? value : throw new JsonException();
    }

    public override void Write(
        Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(value.ToString());
}
