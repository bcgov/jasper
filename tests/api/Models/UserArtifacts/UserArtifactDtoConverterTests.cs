using System;
using System.Collections.Generic;
using System.IO;
using FluentAssertions;
using Newtonsoft.Json;
using Scv.Models.UserArtifacts;
using Xunit;

namespace tests.api.Models.UserArtifacts;

public class UserArtifactDtoConverterTests
{
    private readonly UserArtifactDtoConverter _converter = new();

    private static JsonSerializerSettings GetSettings() => new()
    {
        Converters = { new UserArtifactDtoConverter() }
    };

    [Theory]
    [InlineData(typeof(UserArtifactDto), true)]
    [InlineData(typeof(NoteDto), true)]
    [InlineData(typeof(AnnotationDto), true)]
    [InlineData(typeof(string), false)]
    [InlineData(typeof(object), false)]
    public void CanConvert_Should_Match_UserArtifactDto_Types(Type type, bool expected)
    {
        _converter.CanConvert(type).Should().Be(expected);
    }

    [Fact]
    public void CanWrite_Should_Be_False()
    {
        _converter.CanWrite.Should().BeFalse();
    }

    [Theory]
    [InlineData("Note")]
    [InlineData("note")]
    [InlineData("NOTE")]
    public void ReadJson_Should_Create_NoteDto(string discriminator)
    {
        var json = $$"""{"artifactType":"{{discriminator}}","content":"hello","userId":"u1","tags":["a","b"]}""";

        var result = JsonConvert.DeserializeObject<UserArtifactDto>(json, GetSettings());

        var note = result.Should().BeOfType<NoteDto>().Subject;
        note.ArtifactType.Should().Be(ArtifactType.Note);
        note.Content.Should().Be("hello");
        note.UserId.Should().Be("u1");
        note.Tags.Should().Equal("a", "b");
    }

    [Theory]
    [InlineData("Annotation")]
    [InlineData("annotation")]
    public void ReadJson_Should_Create_AnnotationDto(string discriminator)
    {
        var json = $$"""{"artifactType":"{{discriminator}}","documentId":"d1","documentHash":"h","isStale":true,"annotationData":"{\"page\":1}"}""";

        var result = JsonConvert.DeserializeObject<UserArtifactDto>(json, GetSettings());

        var annotation = result.Should().BeOfType<AnnotationDto>().Subject;
        annotation.ArtifactType.Should().Be(ArtifactType.Annotation);
        annotation.DocumentId.Should().Be("d1");
        annotation.DocumentHash.Should().Be("h");
        annotation.IsStale.Should().BeTrue();
        annotation.AnnotationData.Should().Be("""{"page":1}""");
    }

    [Fact]
    public void ReadJson_Should_Match_Discriminator_Property_Name_Case_Insensitively()
    {
        var json = """{"ARTIFACTTYPE":"Note","content":"x"}""";

        var result = JsonConvert.DeserializeObject<UserArtifactDto>(json, GetSettings());

        result.Should().BeOfType<NoteDto>();
    }

    [Fact]
    public void ReadJson_Should_Populate_Base_Properties()
    {
        var json = """
        {
            "artifactType": "Note",
            "userId": "user",
            "labels": { "k": "v" },
            "createdDate": "2025-01-02T03:04:05Z",
            "deletedByUserId": "deleter"
        }
        """;

        var result = JsonConvert.DeserializeObject<UserArtifactDto>(json, GetSettings());

        result.UserId.Should().Be("user");
        result.Labels.Should().BeEquivalentTo(new Dictionary<string, string> { ["k"] = "v" });
        result.CreatedDate.Should().Be(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc));
        result.DeletedByUserId.Should().Be("deleter");
        result.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void ReadJson_Should_Deserialize_List_Of_Mixed_Artifacts()
    {
        var json = """[{"artifactType":"Note"},{"artifactType":"Annotation"}]""";

        var result = JsonConvert.DeserializeObject<List<UserArtifactDto>>(json, GetSettings());

        result.Should().HaveCount(2);
        result[0].Should().BeOfType<NoteDto>();
        result[1].Should().BeOfType<AnnotationDto>();
    }

    [Fact]
    public void ReadJson_Should_Return_Null_For_Null_Token()
    {
        var result = JsonConvert.DeserializeObject<UserArtifactDto>("null", GetSettings());

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("""{"content":"x"}""")]
    [InlineData("""{"artifactType":null}""")]
    [InlineData("""{"artifactType":""}""")]
    [InlineData("""{"artifactType":"   "}""")]
    [InlineData("""{"artifactType":"Unknown"}""")]
    public void ReadJson_Should_Throw_When_Discriminator_Missing_Or_Invalid(string json)
    {
        var act = () => JsonConvert.DeserializeObject<UserArtifactDto>(json, GetSettings());

        act.Should().Throw<JsonSerializationException>()
            .WithMessage("*artifactType*is required*");
    }

    [Fact]
    public void ReadJson_Should_Throw_For_Undefined_Numeric_Discriminator()
    {
        // Enum.TryParse accepts any numeric string, so an out-of-range value reaches the switch default.
        var json = """{"artifactType":"99"}""";

        var act = () => JsonConvert.DeserializeObject<UserArtifactDto>(json, GetSettings());

        act.Should().Throw<JsonSerializationException>()
            .WithMessage("Unsupported artifact type '99'.");
    }

    [Fact]
    public void WriteJson_Should_Throw_NotSupportedException()
    {
        using var writer = new JsonTextWriter(new StringWriter());

        var act = () => _converter.WriteJson(writer, new NoteDto(), new JsonSerializer());

        act.Should().Throw<NotSupportedException>()
            .WithMessage("*only supports reading*");
    }

    [Fact]
    public void Serialize_Should_Use_Default_Serializer_When_Converter_Registered()
    {
        var json = JsonConvert.SerializeObject(new NoteDto { Content = "hi" }, GetSettings());

        json.Should().Contain("\"Content\":\"hi\"");
        json.Should().Contain("\"ArtifactType\":0");
    }
}
