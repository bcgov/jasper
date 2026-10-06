namespace Scv.Models.UserArtifacts;

public class AnnotationDto : UserArtifactDto
{
    public AnnotationDto() => ArtifactType = ArtifactType.Annotation;

    public string DocumentId { get; set; }
    public string DocumentHash { get; set; }
    public DateTime? DocumentSourceDate { get; set; }
    public string AnnotationData { get; set; }
    public bool IsStale { get; set; }
}
