namespace Scv.Api.Documents.Conversion;

internal static class OfficePackageLimits
{
    internal const int MaximumEntries = 10_000;
    internal const long MaximumContentTypesBytes = 1024L * 1024;
    internal const long MaximumRelationshipPartBytes = 1024L * 1024;
    internal const long MaximumRelationshipPartsBytes = 8L * 1024 * 1024;
}
