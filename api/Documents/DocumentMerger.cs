using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GdPicture14;
using Scv.Api.Models.Document;

namespace Scv.Api.Documents;

public class DocumentMerger(IDocumentRetriever documentRetriever) : IDocumentMerger
{
    /// <summary>
    /// Merges multiple PDF documents into a single PDF document in base64 format.
    /// </summary>
    /// <param name="documentRequests">An array of document requests to merge documents from</param>
    /// <returns>The merge result</returns>
    public async Task<PdfDocumentResponse> MergeDocuments(PdfDocumentRequest[] documentRequests)
    {
        List<Stream> streamsToMerge = [];

        using GdPictureDocumentConverter gdpictureConverter = new();

        // Retrieve all document streams to merge
        var retrieveTasks = documentRequests
            .Select(documentRetriever.Retrieve);

        var documentStreams = await Task.WhenAll(retrieveTasks);

        streamsToMerge.AddRange(documentStreams);

        MemoryStream outputStream = new();

        var mergeResult = gdpictureConverter.CombineToPDF(streamsToMerge, outputStream, PdfConformance.PDF);
        if (mergeResult != GdPictureStatus.OK)
        {
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
}