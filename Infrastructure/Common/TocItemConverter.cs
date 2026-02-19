using System.Text.Json;
using System.Text.Json.Serialization;
using Domain.ValueObjects;

namespace Infrastructure.Common;

/// <summary>
///     Custom JSON converter for <see cref="TocItem" />.
///     Required because:
///     1. STJ rejects parameterised-constructor binding when the parameter type
///     (IEnumerable&lt;TocItem&gt;?) differs from the property type (ReadOnlyCollection&lt;TocItem&gt;).
///     2. Existing DB rows were written when ContentSrc and Children were private
///     properties — those fields are absent from the stored JSON and must be
///     defaulted gracefully rather than blowing up the constructor guards.
/// </summary>
public sealed class TocItemConverter : JsonConverter<TocItem>
{
    public override TocItem Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException($"Expected StartObject token, got {reader.TokenType}.");

        string? id = null;
        string? title = null;
        string? contentSrc = null;
        var playOrder = 0;
        var depth = 0;
        List<TocItem>? children = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
                break;

            if (reader.TokenType != JsonTokenType.PropertyName)
                throw new JsonException("Expected PropertyName token.");

            var propertyName = reader.GetString()!;
            reader.Read(); // move to value token

            switch (propertyName.ToLowerInvariant())
            {
                case "id":
                    id = reader.GetString();
                    break;
                case "title":
                    title = reader.GetString();
                    break;
                case "contentsrc":
                    contentSrc = reader.GetString();
                    break;
                case "playorder":
                    playOrder = reader.GetInt32();
                    break;
                case "depth":
                    depth = reader.GetInt32();
                    break;
                case "children":
                    if (reader.TokenType != JsonTokenType.Null)
                        children = JsonSerializer.Deserialize<List<TocItem>>(ref reader, options);
                    break;
                default:
                    // Skip unknown or computed properties (HasChildren, HasContent, etc.)
                    reader.Skip();
                    break;
            }
        }

        // Graceful fallbacks for data stored before ContentSrc / Children were public.
        // An empty ContentSrc is valid — TocItem allows grouping/header items with no link.
        return new TocItem(
            id ?? string.Empty,
            title ?? string.Empty,
            contentSrc ?? string.Empty,
            playOrder,
            depth,
            children);
    }

    public override void Write(
        Utf8JsonWriter writer,
        TocItem value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", value.Id);
        writer.WriteString("Title", value.Title);
        writer.WriteString("ContentSrc", value.ContentSrc);
        writer.WriteNumber("PlayOrder", value.PlayOrder);
        writer.WriteNumber("Depth", value.Depth);

        writer.WritePropertyName("Children");
        JsonSerializer.Serialize(writer, value.Children, options);

        writer.WriteEndObject();
    }
}