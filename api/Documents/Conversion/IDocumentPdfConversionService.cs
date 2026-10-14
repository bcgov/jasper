using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Scv.Api.Documents.Conversion;

public interface IDocumentPdfConversionService
{
    /// <summary>
    /// Converts a caller-owned input stream to a rewound PDF stream. The caller remains responsible
    /// for the input stream and assumes ownership of the returned stream.
    /// </summary>
    Task<MemoryStream> ConvertToPdfAsync(
        DocumentPdfConversionRequest request,
        CancellationToken cancellationToken = default);
}
