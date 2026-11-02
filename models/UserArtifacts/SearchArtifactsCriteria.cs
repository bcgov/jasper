namespace Scv.Models.UserArtifacts;

public class SearchArtifactsCriteria
{
    public ArtifactType? ArtifactType { get; set; }
    /// <summary>
    /// Label/context key-value pairs that must match exactly (e.g. "caseId") to find artifacts attached to a given context.
    /// </summary>
    public Dictionary<string, string> LabelMatches { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? UpdatedAfter { get; set; }
    public DateTime? UpdatedBefore { get; set; }
    public bool IncludeDeleted { get; set; }
}
