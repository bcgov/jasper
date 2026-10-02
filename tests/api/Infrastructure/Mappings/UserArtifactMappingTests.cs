using System;
using System.Collections.Generic;
using Mapster;
using Scv.Api.Infrastructure.Mappings;
using Xunit;

namespace tests.api.Infrastructure.Mappings;

public class UserArtifactMappingTests
{
    private readonly TypeAdapterConfig _config;

    public UserArtifactMappingTests()
    {
        _config = new TypeAdapterConfig();
        UserArtifactMapping.Register(_config);
    }

    [Fact]
    public void NoteDto_To_UserArtifact_Should_Map_To_Concrete_Note_With_Note_Properties()
    {
        UserArtifactDto dto = new NoteDto
        {
            Id = "note-123",
            UserId = "user-1",
            Content = "Some note content",
            Tags = ["tag1", "tag2"],
            RelatedDate = new DateTime(2025, 1, 1),
            Labels = new Dictionary<string, string> { { "caseId", "123" } }
        };

        var mapper = new MapsterMapper.Mapper(_config);
        var entity = mapper.Map<UserArtifactDto, UserArtifact>(dto);

        var note = Assert.IsType<Note>(entity);
        Assert.NotEqual("note-123", note.Id);
        Assert.Equal("user-1", note.UserId);
        Assert.Equal("Some note content", note.Content);
        Assert.Equal(["tag1", "tag2"], note.Tags);
        Assert.Equal(ArtifactType.Note, note.ArtifactType);
    }

    [Fact]
    public void AnnotationDto_To_UserArtifact_Should_Map_To_Concrete_Annotation_With_Annotation_Properties()
    {
        UserArtifactDto dto = new AnnotationDto
        {
            UserId = "user-1",
            DocumentId = "doc-1",
            DocumentHash = "hash-1",
            IsStale = true
        };

        var mapper = new MapsterMapper.Mapper(_config);
        var entity = mapper.Map<UserArtifactDto, UserArtifact>(dto);

        var annotation = Assert.IsType<Annotation>(entity);
        Assert.Equal("doc-1", annotation.DocumentId);
        Assert.Equal("hash-1", annotation.DocumentHash);
        Assert.True(annotation.IsStale);
        Assert.Equal(ArtifactType.Annotation, annotation.ArtifactType);
    }

    [Fact]
    public void Note_To_UserArtifactDto_Should_Map_To_Concrete_NoteDto()
    {
        UserArtifact entity = new Note
        {
            Id = "note-123",
            UserId = "user-1",
            Content = "Some note content",
            Tags = ["tag1"],
            Ent_Dtm = new DateTime(2025, 1, 1),
            Upd_Dtm = new DateTime(2025, 1, 2)
        };

        var mapper = new MapsterMapper.Mapper(_config);
        var dto = mapper.Map<UserArtifact, UserArtifactDto>(entity);

        var noteDto = Assert.IsType<NoteDto>(dto);
        Assert.Equal("note-123", noteDto.Id);
        Assert.Equal("Some note content", noteDto.Content);
        Assert.Equal(["tag1"], noteDto.Tags);
        Assert.Equal(new DateTime(2025, 1, 1), noteDto.CreatedDate);
        Assert.Equal(new DateTime(2025, 1, 2), noteDto.UpdatedDate);
    }

    [Fact]
    public void NoteDto_Mapped_Onto_Existing_Note_Instance_Should_Populate_Note_Properties()
    {
        UserArtifactDto dto = new NoteDto
        {
            Id = "note-123",
            UserId = "user-1",
            Content = "Updated content",
            Tags = ["tag-a"]
        };

        UserArtifact existing = new Note
        {
            Id = "note-123",
            UserId = "user-1",
            Content = "Original content"
        };

        var mapper = new MapsterMapper.Mapper(_config);
        mapper.Map(dto, existing);

        var note = Assert.IsType<Note>(existing);
        Assert.Equal("Updated content", note.Content);
        Assert.Equal(["tag-a"], note.Tags);
        Assert.Equal("user-1", note.UserId);
    }

    [Fact]
    public void List_Of_UserArtifact_Mapped_Item_By_Item_Should_Preserve_Concrete_Types()
    {
        var entities = new List<UserArtifact>
        {
            new Note { Id = "n1", Content = "note content" },
            new Annotation { Id = "a1", DocumentId = "doc-1" }
        };

        var mapper = new MapsterMapper.Mapper(_config);
        var dtos = entities.ConvertAll(e => mapper.Map<UserArtifact, UserArtifactDto>(e));

        Assert.IsType<NoteDto>(dtos[0]);
        Assert.IsType<AnnotationDto>(dtos[1]);
        Assert.Equal("note content", ((NoteDto)dtos[0]).Content);
        Assert.Equal("doc-1", ((AnnotationDto)dtos[1]).DocumentId);
    }
}
