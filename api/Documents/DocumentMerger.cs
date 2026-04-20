using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GdPicture14;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents;

public class DocumentMerger(
    IDocumentRetriever documentRetriever,
    ILogger<DocumentMerger> logger,
    IConfiguration configuration) : IDocumentMerger
{
    private const int DefaultRetrieveBatchSize = 10;
    private const string RetrieveBatchSizeKey = "DOCUMENT_RETRIEVAL_BATCH_SIZE";

    private readonly IDocumentRetriever documentRetriever = documentRetriever;
    private readonly ILogger<DocumentMerger> logger = logger;
    private readonly IConfiguration configuration = configuration;

    /// <summary>
    /// Merges multiple PDF documents into a single PDF document in base64 format.
    /// </summary>
    /// <param name="documentRequests">An array of document requests to merge documents from</param>
    /// <returns>The merge result</returns>
    public async Task<PdfDocumentResponse> MergeDocuments(PdfDocumentRequest[] documentRequests)
    {
        using GdPictureDocumentConverter gdpictureConverter = new();

        var streamsToMerge = await RetrieveStreamsInBatches(documentRequests);

        try
        {
            // Validate all streams before attempting to merge
            for (int i = 0; i < streamsToMerge.Length; i++)
            {
                var stream = streamsToMerge[i];
                if (stream == null)
                {
                    logger.LogError("Stream {Index} is null for document type {Type}", 
                        i, documentRequests[i].Type);
                    throw new InvalidOperationException($"Document stream {i} is null");
                }

                if (stream.Length == 0)
                {
                    logger.LogError("Stream {Index} is empty for document type {Type}", 
                        i, documentRequests[i].Type);
                    throw new InvalidOperationException($"Document stream {i} is empty");
                }

                logger.LogInformation("Stream {Index} validated - Type: {Type}, Size: {Size} bytes", 
                    i, documentRequests[i].Type, stream.Length);
            }

            var pageRanges = new List<PageRange>();
            var streamsForMerge = new List<Stream>(streamsToMerge.Length);
            int currentPage = 0;

            foreach (var docStream in streamsToMerge)
            {
                docStream.Position = 0;
                using var pdf = new GdPicturePDF();
                pdf.LoadFromStream(docStream, true);
                int pageCount = pdf.GetPageCount();
                pageRanges.Add(new PageRange { Start = currentPage, End = currentPage + pageCount });
                currentPage += pageCount;

                pdf.FlattenFormFields();
                pdf.FlattenVisibleOCGs();

                var flattenedDocStream = new MemoryStream();
                pdf.SaveToStream(flattenedDocStream);
                flattenedDocStream.Position = 0;

                streamsForMerge.Add(flattenedDocStream);

                pdf.CloseDocument();
            }

            MemoryStream outputStream = new();
            var mergeResult = gdpictureConverter.CombineToPDF(streamsForMerge.ToArray(), outputStream, PdfConformance.PDF);
            if (mergeResult != GdPictureStatus.OK)
            {
                logger.LogError("GdPicture merge failed with status: {Status}. Document count: {Count}", 
                    mergeResult, streamsForMerge.Count);
                throw new InvalidOperationException($"Failed to merge documents: {mergeResult}");
            }

            logger.LogInformation("Merged {TotalCount} documents.", streamsForMerge.Count);

            outputStream.Position = 0;

            var response = new PdfDocumentResponse
            {
                Base64Pdf = Convert.ToBase64String(outputStream.ToArray()),
                PageRanges = pageRanges
            };

            return response;
        }
        finally
        {
            // Ensure all streams are disposed
            var disposeTasks = streamsToMerge
                .Where(x => x is not null)
                .Select(stream => stream.DisposeAsync().AsTask());

            await Task.WhenAll(disposeTasks);
        }
    }

    private async Task<MemoryStream[]> RetrieveStreamsInBatches(PdfDocumentRequest[] documentRequests)
    {
        var streams = new MemoryStream[documentRequests.Length];
        var configuredValue = configuration.GetValue<string>(RetrieveBatchSizeKey);
        var batchSize = int.TryParse(configuredValue, out var parsedBatchSize) && parsedBatchSize > 0
            ? parsedBatchSize
            : DefaultRetrieveBatchSize;
        
        for (var batchStart = 0; batchStart < documentRequests.Length; batchStart += batchSize)
        {
            var batchCount = Math.Min(batchSize, documentRequests.Length - batchStart);

            logger.LogInformation(
                "Retrieving document batch starting at index {BatchStart} with {BatchCount} documents.",
                batchStart,
                batchCount);

            var retrieveTasks = Enumerable
                .Range(batchStart, batchCount)
                .Select(index => RetrieveStream(index, documentRequests[index]));

            var batchResults = await Task.WhenAll(retrieveTasks);

            foreach (var (index, stream) in batchResults)
            {
                streams[index] = stream;
            }
        }

        return streams;
    }

    private async Task<(int Index, MemoryStream Stream)> RetrieveStream(int index, PdfDocumentRequest documentRequest)
    {
        var stream = await documentRetriever.Retrieve(documentRequest);
        return (index, stream);
    }
}