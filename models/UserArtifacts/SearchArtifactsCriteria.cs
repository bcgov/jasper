namespace Scv.Models.UserArtifacts;

/// <summary>
/// Criteria for the unified Note/Annotation search endpoint.
/// </summary>
public class SearchArtifactsCriteria
{
    /// <summary>
    /// Restrict results to a single artifact type. Leave null to search both Notes and Annotations.
    /// </summary>
    //public ArtifactType? ArtifactType { get; set; }

    /// <summary>
    /// Label/context keys that must exist (e.g. "caseId") to find artifacts attached to a given context.
    /// </summary>
    public List<string> LabelKeysExist { get; set; } = [];

    /// <summary>
    /// Label/context key-value pairs that must match exactly.
    /// </summary>
    public Dictionary<string, string> LabelMatches { get; set; } = [];

    /// <summary>
    /// Notes containing any of these tags.
    /// </summary>
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// Case-insensitive keyword search against Note content.
    /// </summary>
    public string Keyword { get; set; }

    public DateTime? CreatedAfter { get; set; }
    public DateTime? CreatedBefore { get; set; }
    public DateTime? UpdatedAfter { get; set; }
    public DateTime? UpdatedBefore { get; set; }

    /// <summary>
    /// Include soft-deleted artifacts in the results. Defaults to false.
    /// </summary>
    public bool IncludeDeleted { get; set; }

    /// <summary>
    /// Maximum number of results to return. Default is 100. Set to null for unlimited.
    /// </summary>
    public int? Limit { get; set; } = 100;

    /// <summary>
    /// Number of results to skip (for pagination).
    /// </summary>
    public int? Skip { get; set; }
}
