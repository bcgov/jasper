using System;
using System.Collections.Generic;

namespace Scv.Models.Document;

public sealed class TransitoryDocumentSearchResponse
{
    public required IReadOnlyList<FileMetadataDto> Documents { get; init; }
    public required DateTimeOffset RetrievedAtUtc { get; init; }
    public required bool IsCached { get; init; }
}