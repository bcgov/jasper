using System.Text.Json.Serialization;

namespace Scv.Models.UserArtifacts;

public enum ArtifactType
{
    Note,
    Annotation
}

[JsonConverter(typeof(UserArtifactDtoConverter))]
public abstract class UserArtifactDto : BaseDto
{
    public string UserId { get; set; }
    public ArtifactType ArtifactType { get; set; }
    public Dictionary<string, string> Labels { get; set; } = [];
    public bool IsDeleted => DeletedDate != null;
    public DateTime? DeletedDate { get; set; }
    public string DeletedByUserId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
