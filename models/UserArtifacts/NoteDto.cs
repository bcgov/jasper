namespace Scv.Models.UserArtifacts;

public class NoteDto : UserArtifactDto
{
    public override ArtifactType ArtifactType => ArtifactType.Note;
    public string Content { get; set; }
    public DateTime? RelatedDate { get; set; }
    public List<string> Tags { get; set; } = [];
}
