using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quote;

public class DateOnlyJsonConverter : JsonConverter<DateOnly>
{
    public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && DateTimeOffset.TryParse(reader.GetString(), out var dateTimeOffset))
        {
            return DateOnly.FromDateTime(dateTimeOffset.DateTime);
        }

        throw new JsonException($"Unable to convert \"{reader.GetString()}\" to {nameof(DateOnly)}.");
    }

    public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToDateTime(TimeOnly.MinValue).ToString("O"));
    }
}