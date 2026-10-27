using Newtonsoft.Json;

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
    public abstract ArtifactType ArtifactType { get; }
    public Dictionary<string, string> Labels { get; set; } = [];
    public bool IsDeleted => DeletedDate != null;
    public DateTime? DeletedDate { get; set; }
    public string DeletedByUserId { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
