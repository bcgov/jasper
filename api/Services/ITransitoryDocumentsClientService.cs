using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TDCommon.Clients.DocumentsServices;

namespace Scv.Api.Services;

/// <summary>
/// Interface for wrapping TransitoryDocumentsClient to enable dependency injection and testing.
/// Provides methods for searching and retrieving transitory documents.
/// </summary>
public interface ITransitoryDocumentsClientService
{
    /// <summary>
    /// Sets the bearer token for authentication with the transitory documents service.
    /// </summary>
    /// <param name="token">The bearer token to use for authentication.</param>
    void SetBearerToken(string token);

    /// <summary>
    /// Searches for transitory documents based on the provided search criteria.
    /// </summary>
    /// <param name="body">The search request containing criteria for finding documents.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A collection of file metadata matching the search criteria.</returns>
    Task<ICollection<FileMetadataDto>> SearchAsync(
        TransitoryDocumentSearchRequest body,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the content of a transitory document at the specified path.
    /// </summary>
    /// <param name="path">The path to the document to retrieve.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A file response containing the document content.</returns>
    Task<FileResponse> ContentAsync(string path, CancellationToken cancellationToken = default);
}
