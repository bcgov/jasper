using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Scv.Models.UserArtifacts;

/// <summary>
/// Deserializes a request body into the concrete <see cref="NoteDto"/> or <see cref="AnnotationDto"/> type
/// based on its "artifactType" discriminator field, so a single controller action/parameter typed as
/// <see cref="UserArtifactDto"/> can accept either shape. Writing (serialization) is left to the default
/// contract resolver so only the properties relevant to the concrete runtime type are emitted.
/// </summary>
public class UserArtifactDtoConverter : JsonConverter
{
    private const string DiscriminatorField = "artifactType";

    public override bool CanConvert(Type objectType) => typeof(UserArtifactDto).IsAssignableFrom(objectType);

    public override bool CanWrite => false;

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
        {
            return null;
        }

        var jObject = JObject.Load(reader);
        var discriminator = jObject.GetValue(DiscriminatorField, StringComparison.OrdinalIgnoreCase)?.ToString();

        if (string.IsNullOrWhiteSpace(discriminator) || !Enum.TryParse<ArtifactType>(discriminator, true, out var artifactType))
        {
            throw new JsonSerializationException(
                $"A valid '{DiscriminatorField}' ('{nameof(ArtifactType.Note)}' or '{nameof(ArtifactType.Annotation)}') is required.");
        }

        UserArtifactDto target = artifactType switch
        {
            ArtifactType.Note => new NoteDto(),
            ArtifactType.Annotation => new AnnotationDto(),
            _ => throw new JsonSerializationException($"Unsupported artifact type '{discriminator}'.")
        };

        serializer.Populate(jObject.CreateReader(), target);

        return target;
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) =>
        throw new NotSupportedException($"{nameof(UserArtifactDtoConverter)} only supports reading; writing uses the default serializer.");
}
