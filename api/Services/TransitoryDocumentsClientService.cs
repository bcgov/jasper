using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TDCommon.Clients.DocumentsServices;

namespace Scv.Api.Services;

/// <summary>
/// Wrapper implementation for TransitoryDocumentsClient that delegates calls to the underlying NSwag-generated client.
/// This wrapper enables dependency injection and testing.
/// </summary>
public class TransitoryDocumentsClientService : ITransitoryDocumentsClientService
{
    private readonly TransitoryDocumentsClient _client;

    public TransitoryDocumentsClientService(TransitoryDocumentsClient client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public void SetBearerToken(string token)
    {
        _client.SetBearerToken(token);
    }

    /// <inheritdoc />
    public async Task<ICollection<FileMetadataDto>> SearchAsync(
        TransitoryDocumentSearchRequest body,
        CancellationToken cancellationToken = default)
    {
        return await _client.SearchAsync(body, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FileResponse> ContentAsync(string path, CancellationToken cancellationToken = default)
    {
        return await _client.ContentAsync(path, cancellationToken);
    }
}
