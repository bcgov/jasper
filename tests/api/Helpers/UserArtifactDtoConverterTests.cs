using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Xunit;

namespace tests.Api.Helpers;

public class UserArtifactDtoConverterTests
{
    private static JsonSerializerSettings SettingsWithCamelCase()
    {
        return new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy() }
        };
    }

    [Fact]
    public void Deserialize_WithArtifactTypeNote_ReturnsNoteDto()
    {
        var json = @"{ ""artifactType"": ""Note"", ""content"": ""hello"", ""tags"": [""a"", ""b""] }";

        var dto = JsonConvert.DeserializeObject<UserArtifactDto>(json, SettingsWithCamelCase());

        var note = Assert.IsType<NoteDto>(dto);
        Assert.Equal("hello", note.Content);
        Assert.Equal(["a", "b"], note.Tags);
    }

    [Fact]
    public void Deserialize_WithArtifactTypeAnnotation_ReturnsAnnotationDto()
    {
        var json = @"{ ""artifactType"": ""Annotation"", ""documentId"": ""doc-1"", ""documentHash"": ""hash-1"" }";

        var dto = JsonConvert.DeserializeObject<UserArtifactDto>(json, SettingsWithCamelCase());

        var annotation = Assert.IsType<AnnotationDto>(dto);
        Assert.Equal("doc-1", annotation.DocumentId);
        Assert.Equal("hash-1", annotation.DocumentHash);
    }

    [Fact]
    public void Deserialize_IsCaseInsensitiveOnArtifactTypeValue()
    {
        var json = @"{ ""artifactType"": ""note"", ""content"": ""hello"" }";

        var dto = JsonConvert.DeserializeObject<UserArtifactDto>(json, SettingsWithCamelCase());

        Assert.IsType<NoteDto>(dto);
    }

    [Fact]
    public void Deserialize_WithMissingArtifactType_Throws()
    {
        var json = @"{ ""content"": ""hello"" }";

        Assert.Throws<JsonSerializationException>(() =>
            JsonConvert.DeserializeObject<UserArtifactDto>(json, SettingsWithCamelCase()));
    }

    [Fact]
    public void Deserialize_WithUnknownArtifactType_Throws()
    {
        var json = @"{ ""artifactType"": ""Unknown"" }";

        Assert.Throws<JsonSerializationException>(() =>
            JsonConvert.DeserializeObject<UserArtifactDto>(json, SettingsWithCamelCase()));
    }

    [Fact]
    public void Serialize_NoteDto_OnlyEmitsNoteProperties()
    {
        UserArtifactDto dto = new NoteDto { Id = "note-1", Content = "hello" };

        var json = JsonConvert.SerializeObject(dto, SettingsWithCamelCase());

        Assert.Contains("\"content\":\"hello\"", json);
        Assert.DoesNotContain("documentId", json);
    }
}
