using System;
using System.Collections.Generic;
using System.Linq;
using Scv.Models.Document;

namespace Scv.Api.Documents;

public class PdfMergePreparationStrategyResolver(
    IEnumerable<IPdfMergePreparationStrategy> strategies) : IPdfMergePreparationStrategyResolver
{
    private readonly IEnumerable<IPdfMergePreparationStrategy> _strategies = strategies;

    public PdfMergePreparationMode Resolve(PdfDocumentRequest documentRequest)
    {
        ArgumentNullException.ThrowIfNull(documentRequest);

        var matches = _strategies
            .Where(strategy => strategy.CanHandle(documentRequest))
            .ToList();

        return matches.Count switch
        {
            1 => matches[0].PreparationMode,
            0 => throw new InvalidOperationException($"No PDF merge preparation strategy is registered for document type {documentRequest.Type}."),
            _ => throw new InvalidOperationException($"Multiple PDF merge preparation strategies are registered for document type {documentRequest.Type}.")
        };
    }
}