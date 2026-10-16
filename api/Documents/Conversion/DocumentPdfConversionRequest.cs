using System.IO;

namespace Scv.Api.Documents.Conversion;

public sealed record DocumentPdfConversionRequest(
    Stream Stream,
    string FileName,
    string SourceIdentifier);
