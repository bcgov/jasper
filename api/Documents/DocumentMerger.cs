using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GdPicture14;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Models.Document;

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
            ValidateRetrievedStreams(streamsToMerge, documentRequests);
            var pageRanges = new List<PageRange>();
            var streamsForMerge = new List<MemoryStream>(streamsToMerge.Length);

            try
            {
                PopulateFlattenedStreams(streamsToMerge, documentRequests, streamsForMerge, pageRanges);

                using var outputStream = MergeStreams(gdpictureConverter, streamsForMerge);
                return CreateResponse(outputStream, pageRanges);
            }
            finally
            {
                // Ensure to dispose all flattened streams created during merge preparation
                var disposeFlattenedTasks = streamsForMerge
                    .Select(s => s.DisposeAsync().AsTask());

                await Task.WhenAll(disposeFlattenedTasks);
            }
        }
        finally
        {
            // Ensure all retrieved streams are disposed
            var disposeTasks = streamsToMerge
                .Where(x => x is not null)
                .Select(stream => stream.DisposeAsync().AsTask());

            await Task.WhenAll(disposeTasks);
        }
    }

    private void ValidateRetrievedStreams(MemoryStream[] streamsToMerge, PdfDocumentRequest[] documentRequests)
    {
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

            var looksLikePdf = HasPdfSignature(stream);
            var headerHex = ReadHeaderHex(stream);

            logger.LogInformation(
                "Stream {Index} validated - Type: {Type}, Size: {Size} bytes, LooksLikePdf: {LooksLikePdf}, HeaderHex: {HeaderHex}",
                i,
                documentRequests[i].Type,
                stream.Length,
                looksLikePdf,
                headerHex);

            if (!looksLikePdf)
            {
                logger.LogWarning(
                    "Stream {Index} does not match PDF signature. Type: {Type}, HeaderAsciiPreview: {HeaderAsciiPreview}",
                    i,
                    documentRequests[i].Type,
                    ReadHeaderAsciiPreview(stream));
            }
        }
    }

    private void PopulateFlattenedStreams(
        MemoryStream[] streamsToMerge,
        PdfDocumentRequest[] documentRequests,
        List<MemoryStream> streamsForMerge,
        List<PageRange> pageRanges)
    {
        int currentPage = 0;

        for (int i = 0; i < streamsToMerge.Length; i++)
        {
            var docStream = streamsToMerge[i] ?? throw new InvalidOperationException($"Document stream {i} is null");
            try
            {
                docStream.Position = 0;
                using var pdf = new GdPicturePDF();

                LoadPdfFromStream(pdf, docStream, documentRequests[i], i);
                var pageCount = ValidatePageCount(pdf, documentRequests[i], i);
                pageRanges.Add(new PageRange { Start = currentPage, End = currentPage + pageCount });
                currentPage += pageCount;

                FlattenPdfContent(pdf, documentRequests[i], i);
                streamsForMerge.Add(SaveFlattenedPdfToStream(pdf, documentRequests[i], i));
            }
            finally
            {
                // Release original retrieved stream as soon as it is no longer needed.
                docStream.Dispose();
                streamsToMerge[i] = null!;
            }
        }
    }

    private void LoadPdfFromStream(
        GdPicturePDF pdf,
        MemoryStream docStream,
        PdfDocumentRequest documentRequest,
        int index)
    {
        var loadStatus = pdf.LoadFromStream(docStream, true);
        if (loadStatus != GdPictureStatus.OK)
        {
            logger.LogError(
                "GdPicture failed to load stream {Index} (Type: {Type}) with status: {Status}. Size: {Size}, LooksLikePdf: {LooksLikePdf}, HeaderHex: {HeaderHex}, HeaderAsciiPreview: {HeaderAsciiPreview}",
                index,
                documentRequest.Type,
                loadStatus,
                docStream.Length,
                HasPdfSignature(docStream),
                ReadHeaderHex(docStream),
                ReadHeaderAsciiPreview(docStream));
            throw new InvalidOperationException(
                $"Failed to load document stream {index} (Type: {documentRequest.Type}): {loadStatus}");
        }
    }

    private static bool HasPdfSignature(MemoryStream stream)
    {
        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            if (stream.Length < 5)
            {
                return false;
            }

            Span<byte> prefix = stackalloc byte[5];
            var bytesRead = stream.Read(prefix);
            return bytesRead == 5
                && prefix[0] == '%'
                && prefix[1] == 'P'
                && prefix[2] == 'D'
                && prefix[3] == 'F'
                && prefix[4] == '-';
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static string ReadHeaderHex(MemoryStream stream, int maxBytes = 16)
    {
        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var buffer = new byte[Math.Min(maxBytes, checked((int)stream.Length))];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            return bytesRead == 0 ? string.Empty : Convert.ToHexString(buffer.AsSpan(0, bytesRead));
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private static string ReadHeaderAsciiPreview(MemoryStream stream, int maxBytes = 64)
    {
        var originalPosition = stream.Position;
        try
        {
            stream.Position = 0;
            var buffer = new byte[Math.Min(maxBytes, checked((int)stream.Length))];
            var bytesRead = stream.Read(buffer, 0, buffer.Length);
            if (bytesRead == 0)
            {
                return string.Empty;
            }

            var raw = Encoding.UTF8.GetString(buffer, 0, bytesRead);
            return new string(raw.Select(c => char.IsControl(c) && c is not '\r' and not '\n' and not '\t' ? '.' : c).ToArray());
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }

    private int ValidatePageCount(GdPicturePDF pdf, PdfDocumentRequest documentRequest, int index)
    {
        int pageCount = pdf.GetPageCount();
        if (pageCount <= 0)
        {
            logger.LogError(
                "Stream {Index} (Type: {Type}) reported {PageCount} pages after loading",
                index, documentRequest.Type, pageCount);
            throw new InvalidOperationException(
                $"Document stream {index} (Type: {documentRequest.Type}) contains no pages");
        }

        return pageCount;
    }

    private void FlattenPdfContent(GdPicturePDF pdf, PdfDocumentRequest documentRequest, int index)
    {
        var flattenStatus = pdf.FlattenFormFields();
        if (flattenStatus != GdPictureStatus.OK)
        {
            logger.LogWarning(
                "FlattenFormFields on stream {Index} (Type: {Type}) returned status: {Status} — continuing",
                index, documentRequest.Type, flattenStatus);
        }

        var flattenOcgStatus = pdf.FlattenVisibleOCGs();
        if (flattenOcgStatus != GdPictureStatus.OK)
        {
            logger.LogWarning(
                "FlattenVisibleOCGs on stream {Index} (Type: {Type}) returned status: {Status} — continuing",
                index, documentRequest.Type, flattenOcgStatus);
        }
    }

    private MemoryStream SaveFlattenedPdfToStream(
        GdPicturePDF pdf,
        PdfDocumentRequest documentRequest,
        int index)
    {
        var flattenedDocStream = new MemoryStream();
        var saveStatus = pdf.SaveToStream(flattenedDocStream);
        if (saveStatus != GdPictureStatus.OK)
        {
            flattenedDocStream.Dispose();
            logger.LogError(
                "GdPicture failed to save flattened stream {Index} (Type: {Type}) with status: {Status}",
                index, documentRequest.Type, saveStatus);
            throw new InvalidOperationException(
                $"Failed to save flattened document stream {index} (Type: {documentRequest.Type}): {saveStatus}");
        }

        flattenedDocStream.Position = 0;
        return flattenedDocStream;
    }

    private MemoryStream MergeStreams(GdPictureDocumentConverter gdpictureConverter, List<MemoryStream> streamsForMerge)
    {
        var outputStream = new MemoryStream();
        var mergeResult = gdpictureConverter.CombineToPDF([.. streamsForMerge], outputStream, PdfConformance.PDF);
        if (mergeResult != GdPictureStatus.OK)
        {
            outputStream.Dispose();
            logger.LogError("GdPicture merge failed with status: {Status}. Document count: {Count}",
                mergeResult, streamsForMerge.Count);
            throw new InvalidOperationException($"Failed to merge documents: {mergeResult}");
        }

        logger.LogInformation("Merged {TotalCount} documents.", streamsForMerge.Count);
        outputStream.Position = 0;
        return outputStream;
    }

    private static PdfDocumentResponse CreateResponse(MemoryStream outputStream, List<PageRange> pageRanges)
    {
        var outputLength = checked((int)outputStream.Length);
        var base64Pdf = outputStream.TryGetBuffer(out var buffer)
            ? Convert.ToBase64String(buffer.Array!, buffer.Offset, outputLength)
            : Convert.ToBase64String(outputStream.ToArray());

        return new PdfDocumentResponse
        {
            Base64Pdf = base64Pdf,
            PageRanges = pageRanges
        };
    }

    private async Task<MemoryStream[]> RetrieveStreamsInBatches(PdfDocumentRequest[] documentRequests)
    {
        var streams = new MemoryStream[documentRequests.Length];
        try
        {
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
        catch
        {
            var disposeTasks = streams
                .Where(s => s is not null)
                .Select(s => s.DisposeAsync().AsTask());

            await Task.WhenAll(disposeTasks);
            throw;
        }
    }

    private async Task<(int Index, MemoryStream Stream)> RetrieveStream(int index, PdfDocumentRequest documentRequest)
    {
        var stream = await documentRetriever.Retrieve(documentRequest);
        return (index, stream);
    }
}
