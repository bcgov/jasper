namespace Scv.Models;

public class CaseSyncWarning
{
    public int? JudgeId { get; init; }
    public string PhysicalFileId { get; init; }
    public string AppearanceId { get; init; }
    public int ExistingCount { get; init; }
    public int ImportedCount { get; init; }
    public bool MissingBusinessKey { get; init; }
    public string[] ConflictingOverrides { get; init; } = [];
}
