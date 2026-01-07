using GdPicture14;
using Scv.Models.Document;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Scv.Api.Documents;

public class DocumentMerger(IDocumentRetriever documentRetriever, ILogger<DocumentMerger> logger) : IDocumentMerger
{

    /// <summary>
    /// Merges multiple PDF documents into a single PDF document in base64 format.
    /// </summary>
    /// <param name="documentRequests">An array of document requests to merge documents from</param>
    /// <returns>The merge result</returns>
    public async Task<PdfDocumentResponse> MergeDocuments(PdfDocumentRequest[] documentRequests)
    {
        using GdPictureDocumentConverter gdpictureConverter = new();

        // Retrieve all document streams to merge
        var retrieveTasks = documentRequests
            .Select(documentRetriever.Retrieve);

        var streamsToMerge = await Task.WhenAll(retrieveTasks);

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

            MemoryStream outputStream = new();

            var mergeResult = gdpictureConverter.CombineToPDF(streamsToMerge, outputStream, PdfConformance.PDF);
            if (mergeResult != GdPictureStatus.OK)
            {
                logger.LogError("GdPicture merge failed with status: {Status}. Document count: {Count}", 
                    mergeResult, streamsToMerge.Length);
                throw new InvalidOperationException($"Failed to merge documents: {mergeResult}");
            }

            // Calculate page counts and ranges
            var pageRanges = new List<PageRange>();
            int currentPage = 0;
            foreach (var docStream in streamsToMerge)
            {
                docStream.Position = 0;
                using var pdf = new GdPicturePDF();
                pdf.LoadFromStream(docStream, true);
                int pageCount = pdf.GetPageCount();
                pageRanges.Add(new PageRange { Start = currentPage, End = currentPage + pageCount });
                currentPage += pageCount;
                pdf.CloseDocument();
            }

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
            foreach (var stream in streamsToMerge.Where(x => x is not null))
            {
                await stream.DisposeAsync();
            }
        }
    }
}