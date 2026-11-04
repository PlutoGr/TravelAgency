using System.Text.Json;
using System.Text.Json.Serialization;

namespace TravelAgency.Identity.Application.DTOs;

/// <summary>
/// Снимок четырёх полей визитки. Все ключи обязательны: пропуск не оставляет прежнее значение,
/// иначе null и [] не очистят год и теги. Незнакомое имя и неверный тип отклоняются при разборе.
/// </summary>
[JsonConverter(typeof(UpdateManagerCardRequestConverter))]
public sealed record UpdateManagerCardRequest(
    string? Position,
    int? TourismSince,
    string? About,
    IReadOnlyList<string> Tags);

public sealed class UpdateManagerCardRequestConverter : JsonConverter<UpdateManagerCardRequest>
{
    private static readonly string[] Required = ["position", "tourismSince", "about", "tags"];

    public override UpdateManagerCardRequest? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException();

        using var document = JsonDocument.ParseValue(ref reader);
        EnsureSnapshot(document.RootElement);
        var payload = JsonSerializer.Deserialize<Payload>(document.RootElement.GetRawText(), options)
            ?? throw new JsonException();
        if (payload.Tags is null)
            throw new JsonException();

        return new UpdateManagerCardRequest(payload.Position, payload.TourismSince, payload.About, payload.Tags);
    }

    public override void Write(Utf8JsonWriter writer, UpdateManagerCardRequest value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        WriteString(writer, "position", value.Position);
        if (value.TourismSince is int year)
            writer.WriteNumber("tourismSince", year);
        else
            writer.WriteNull("tourismSince");
        WriteString(writer, "about", value.About);
        writer.WritePropertyName("tags");
        JsonSerializer.Serialize(writer, value.Tags, options);
        writer.WriteEndObject();
    }

    private static void EnsureSnapshot(JsonElement root)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in root.EnumerateObject())
        {
            if (!seen.Add(property.Name))
                throw new JsonException();
            if (!Required.Contains(property.Name, StringComparer.OrdinalIgnoreCase))
                throw new JsonException();
        }

        foreach (var name in Required)
        {
            if (!seen.Contains(name))
                throw new JsonException();
        }

        // Сериализатор MVC читает число из строки. Для визитки тип JSON остаётся строгим.
        EnsureKind(root, "position", JsonValueKind.String, allowNull: true);
        EnsureKind(root, "tourismSince", JsonValueKind.Number, allowNull: true);
        EnsureKind(root, "about", JsonValueKind.String, allowNull: true);
        EnsureKind(root, "tags", JsonValueKind.Array, allowNull: false);
    }

    private static void EnsureKind(JsonElement root, string name, JsonValueKind kind, bool allowNull)
    {
        var value = Find(root, name);
        if (allowNull && value.ValueKind == JsonValueKind.Null)
            return;
        if (value.ValueKind != kind)
            throw new JsonException();
        if (kind == JsonValueKind.Number && !value.TryGetInt32(out _))
            throw new JsonException();
        if (kind != JsonValueKind.Array)
            return;

        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                throw new JsonException();
        }
    }

    private static JsonElement Find(JsonElement root, string name)
    {
        foreach (var property in root.EnumerateObject())
        {
            if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return property.Value;
        }

        throw new JsonException();
    }

    private static void WriteString(Utf8JsonWriter writer, string name, string? value)
    {
        if (value is null)
            writer.WriteNull(name);
        else
            writer.WriteString(name, value);
    }

    private sealed record Payload(
        string? Position,
        int? TourismSince,
        string? About,
        IReadOnlyList<string>? Tags);
}
