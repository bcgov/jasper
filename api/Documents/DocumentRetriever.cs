using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents.Strategies;
using Scv.Models.Document;

namespace Scv.Api.Documents;

public class DocumentRetriever(IEnumerable<IDocumentStrategy> strategies, ILogger<DocumentRetriever> logger) : IDocumentRetriever
{
    private readonly IEnumerable<IDocumentStrategy> _strategies = strategies;
    private readonly ILogger<DocumentRetriever> _logger = logger;

    /// <summary>
    /// Retrieves a PDF stream from a given source based off the document-request type.
    /// </summary>
    /// <param name="documentRequest">The document request containing the source information</param>
    /// <returns>The retrieved PDF document stream</returns>
    public Task<MemoryStream> Retrieve(PdfDocumentRequest documentRequest)
    {
        return Retrieve(documentRequest, CancellationToken.None);
    }

    public Task<MemoryStream> Retrieve(
        PdfDocumentRequest documentRequest,
        CancellationToken cancellationToken)
    {
        try
        {
            var strategy = _strategies.FirstOrDefault(strat => strat.Type == documentRequest.Type)
                ?? throw new InvalidOperationException();
            return strategy.Invoke(documentRequest.Data, cancellationToken);
        }
        catch (InvalidOperationException)
        {
            _logger.LogError("No matching strategy found for document type: {DocumentType}", documentRequest.Type);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occured while retrieving document information.");
            throw;
        }
    }
}
