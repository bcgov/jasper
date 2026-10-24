using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GdPicture14;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.Api.Documents.Conversion;
using Scv.Api.Infrastructure.Options;
using Scv.Models.Document;

namespace Scv.Api.Documents;

public class DocumentMerger(
    IDocumentRetriever documentRetriever,
    IPdfMergePreparationStrategyResolver preparationStrategyResolver,
    ILogger<DocumentMerger> logger,
    IConfiguration configuration,
    IOptions<DocumentConversionOptions> conversionOptions) : IDocumentMerger
{
    private const int DefaultRetrieveBatchSize = 10;
    private const string RetrieveBatchSizeKey = "DOCUMENT_RETRIEVAL_BATCH_SIZE";

    private readonly IDocumentRetriever documentRetriever = documentRetriever;
    private readonly IPdfMergePreparationStrategyResolver preparationStrategyResolver = preparationStrategyResolver;
    private readonly ILogger<DocumentMerger> logger = logger;
    private readonly IConfiguration configuration = configuration;
    private readonly DocumentConversionOptions conversionOptions = conversionOptions.Value;

    /// <summary>
    /// Merges multiple PDF documents into a single PDF document in base64 format.
    /// </summary>
    /// <param name="documentRequests">An array of document requests to merge documents from</param>
    /// <returns>The merge result</returns>
    public async Task<PdfDocumentResponse> MergeDocuments(PdfDocumentRequest[] documentRequests)
    {
        return await MergeDocumentsCore(documentRequests, CancellationToken.None);
    }

    public async Task<PdfDocumentResponse> MergeDocuments(
        PdfDocumentRequest[] documentRequests,
        CancellationToken cancellationToken)
    {
        return await MergeDocumentsCore(documentRequests, cancellationToken);
    }

    private async Task<PdfDocumentResponse> MergeDocumentsCore(
        PdfDocumentRequest[] documentRequests,
        CancellationToken cancellationToken)
    {
        return ContainsTransitoryDocuments(documentRequests)
            ? await MergeConversionAwareDocuments(documentRequests, cancellationToken)
            : await MergeExistingPdfDocuments(documentRequests, cancellationToken);
    }

    private static bool ContainsTransitoryDocuments(PdfDocumentRequest[] documentRequests) =>
        documentRequests.Any(request => request.Type == Scv.Models.Document.DocumentType.TransitoryDocument);

    private async Task<PdfDocumentResponse> MergeConversionAwareDocuments(
        PdfDocumentRequest[] documentRequests,
        CancellationToken cancellationToken)
    {
        var pageRanges = new List<PageRange>();
        MemoryStream mergedStream = null;
        var currentPage = 0;
        var enforceTransitoryPageLimit = ShouldEnforceTransitoryPageLimit(documentRequests);
        long aggregateConvertedBytes = 0;
        try
        {
            await RetrieveStreamsInBatches(
                documentRequests,
                cancellationToken,
                async (index, stream) =>
                {
                    aggregateConvertedBytes = CountConvertedBytes(
                        aggregateConvertedBytes, stream.Length, documentRequests[index].Type);
                    ValidateRetrievedStream(stream, documentRequests[index], index);
                    cancellationToken.ThrowIfCancellationRequested();
                    using var pdf = new GdPicturePDF();
                    stream.Position = 0;
                    LoadPdfFromStream(pdf, stream, documentRequests[index], index);
                    var pageCount = ValidatePageCount(pdf, documentRequests[index], index);
                    if (enforceTransitoryPageLimit &&
                        currentPage + pageCount > conversionOptions.MaxPageCount)
                    {
                        throw new Scv.Core.Exceptions.DocumentSizeLimitExceededException(
                            DocumentPdfConversionService.ConversionErrorMessage);
                    }

                    pageRanges.Add(new PageRange { Start = currentPage, End = currentPage + pageCount });
                    currentPage += pageCount;
                    PreparePdfForMerge(pdf, documentRequests[index], index);
                    using var preparedStream = documentRequests[index].Type == Scv.Models.Document.DocumentType.TransitoryDocument
                        ? SaveConvertedPdfToBoundedStream(pdf, documentRequests[index], index)
                        : SavePdfToStream(pdf, documentRequests[index], index);
                    mergedStream = MergeIntoAccumulator(mergedStream, preparedStream);
                    await Task.CompletedTask;
                });

            cancellationToken.ThrowIfCancellationRequested();
            if (mergedStream is null)
            {
                throw new InvalidOperationException("No documents were available to merge.");
            }

            return CreateConversionAwareResponse(mergedStream, pageRanges);
        }
        finally
        {
            if (mergedStream is not null)
            {
                await mergedStream.DisposeAsync();
            }
        }
    }

    private async Task<PdfDocumentResponse> MergeExistingPdfDocuments(
        PdfDocumentRequest[] documentRequests,
        CancellationToken cancellationToken)
    {
        using var converter = new GdPictureDocumentConverter();
        var streamsToMerge = await RetrieveExistingPdfStreamsInBatches(documentRequests, cancellationToken);

        try
        {
            for (var index = 0; index < streamsToMerge.Length; index++)
            {
                ValidateRetrievedStream(streamsToMerge[index], documentRequests[index], index);
            }

            var pageRanges = new List<PageRange>();
            var streamsForMerge = new List<MemoryStream>(streamsToMerge.Length);
            var currentPage = 0;
            try
            {
                for (var index = 0; index < streamsToMerge.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var stream = streamsToMerge[index];
                    try
                    {
                        stream.Position = 0;
                        using var pdf = new GdPicturePDF();
                        LoadPdfFromStream(pdf, stream, documentRequests[index], index);
                        var pageCount = ValidatePageCount(pdf, documentRequests[index], index);
                        pageRanges.Add(new PageRange { Start = currentPage, End = currentPage + pageCount });
                        currentPage += pageCount;
                        PreparePdfForMerge(pdf, documentRequests[index], index);
                        streamsForMerge.Add(SavePdfToStream(pdf, documentRequests[index], index));
                    }
                    finally
                    {
                        stream.Dispose();
                        streamsToMerge[index] = null;
                    }
                }

                cancellationToken.ThrowIfCancellationRequested();
                using var outputStream = MergeExistingPdfStreams(converter, streamsForMerge);
                return CreateResponse(outputStream, pageRanges);
            }
            finally
            {
                await Task.WhenAll(streamsForMerge.Select(stream => stream.DisposeAsync().AsTask()));
            }
        }
        finally
        {
            await Task.WhenAll(streamsToMerge
                .Where(stream => stream is not null)
                .Select(stream => stream.DisposeAsync().AsTask()));
        }
    }

    private MemoryStream MergeExistingPdfStreams(
        GdPictureDocumentConverter converter,
        List<MemoryStream> streamsForMerge)
    {
        var outputStream = new MemoryStream();
        var mergeResult = converter.CombineToPDF([.. streamsForMerge], outputStream, PdfConformance.PDF);
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

    private async Task<MemoryStream[]> RetrieveExistingPdfStreamsInBatches(
        PdfDocumentRequest[] documentRequests,
        CancellationToken cancellationToken)
    {
        var streams = new MemoryStream[documentRequests.Length];
        var batchSize = GetRetrievalBatchSize();
        using var retrievalCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        try
        {
            for (var batchStart = 0; batchStart < documentRequests.Length; batchStart += batchSize)
            {
                retrievalCancellation.Token.ThrowIfCancellationRequested();
                var batchCount = Math.Min(batchSize, documentRequests.Length - batchStart);
                var tasks = Enumerable.Range(batchStart, batchCount)
                    .Select(index => RetrieveStream(index, documentRequests[index],
                        retrievalCancellation.Token, retrievalCancellation))
                    .ToArray();
                (int Index, MemoryStream Stream)[] batch;
                try
                {
                    batch = await Task.WhenAll(tasks);
                }
                catch
                {
                    await Task.WhenAll(tasks
                        .Where(task => task.IsCompletedSuccessfully)
                        .Select(task => task.Result.Stream.DisposeAsync().AsTask()));
                    throw;
                }

                foreach (var (index, stream) in batch)
                {
                    streams[index] = stream;
                }
            }

            return streams;
        }
        catch
        {
            await Task.WhenAll(streams
                .Where(stream => stream is not null)
                .Select(stream => stream.DisposeAsync().AsTask()));
            throw;
        }
    }

    internal static bool ShouldEnforceTransitoryPageLimit(PdfDocumentRequest[] documentRequests)
    {
        return documentRequests.Length > 0 &&
            documentRequests.All(request =>
                request.Type == Scv.Models.Document.DocumentType.TransitoryDocument);
    }

    private void ValidateRetrievedStream(MemoryStream stream, PdfDocumentRequest documentRequest, int index)
    {
        if (stream is null)
        {
            logger.LogError("Stream {Index} is null for document type {Type}", index, documentRequest.Type);
            throw new InvalidOperationException($"Document stream {index} is null");
        }

        if (stream.Length == 0)
        {
            logger.LogError("Stream {Index} is empty for document type {Type}", index, documentRequest.Type);
            throw new InvalidOperationException($"Document stream {index} is empty");
        }

        var looksLikePdf = PdfFileValidation.HasPdfSignature(stream);
        logger.LogInformation(
            "Stream {Index} validated - Type: {Type}, Size: {Size} bytes, LooksLikePdf: {LooksLikePdf}, HeaderHex: {HeaderHex}",
            index,
            documentRequest.Type,
            stream.Length,
            looksLikePdf,
            ReadHeaderHex(stream));
        if (!looksLikePdf)
        {
            logger.LogWarning(
                "Stream {Index} does not match PDF signature. Type: {Type}, HeaderAsciiPreview: {HeaderAsciiPreview}",
                index,
                documentRequest.Type,
                ReadHeaderAsciiPreview(stream));
        }
    }

    private void PreparePdfForMerge(GdPicturePDF pdf, PdfDocumentRequest documentRequest, int index)
    {
        var preparationMode = preparationStrategyResolver.Resolve(documentRequest);

        switch (preparationMode)
        {
            case PdfMergePreparationMode.None:
                return;
            case PdfMergePreparationMode.Flatten:
                FlattenPdfContent(pdf, documentRequest, index);
                return;
            default:
                throw new InvalidOperationException($"Unsupported PDF merge preparation mode: {preparationMode}");
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
                PdfFileValidation.HasPdfSignature(docStream),
                ReadHeaderHex(docStream),
                ReadHeaderAsciiPreview(docStream));
            if (documentRequest.Type == Scv.Models.Document.DocumentType.TransitoryDocument)
            {
                throw new Scv.Core.Exceptions.UnprocessableDocumentException(
                    DocumentPdfConversionService.ConversionErrorMessage);
            }

            throw new InvalidOperationException(
                $"Failed to load document stream {index} (Type: {documentRequest.Type}): {loadStatus}");
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

    private MemoryStream SavePdfToStream(
        GdPicturePDF pdf,
        PdfDocumentRequest documentRequest,
        int index) =>
        SavePdfToStream(pdf, documentRequest, index, new MemoryStream());

    private MemoryStream SaveConvertedPdfToBoundedStream(
        GdPicturePDF pdf,
        PdfDocumentRequest documentRequest,
        int index) =>
        SavePdfToStream(pdf, documentRequest, index, CreateBoundedMergeOutputStream());

    private MemoryStream SavePdfToStream(
        GdPicturePDF pdf,
        PdfDocumentRequest documentRequest,
        int index,
        MemoryStream docStream)
    {
        var saveStatus = pdf.SaveToStream(docStream);
        if (saveStatus != GdPictureStatus.OK)
        {
            docStream.Dispose();
            logger.LogError(
                "GdPicture failed to save flattened stream {Index} (Type: {Type}) with status: {Status}",
                index, documentRequest.Type, saveStatus);
            throw new InvalidOperationException(
                $"Failed to save flattened document stream {index} (Type: {documentRequest.Type}): {saveStatus}");
        }

        docStream.Position = 0;
        return docStream;
    }

    private MemoryStream MergeIntoAccumulator(
        MemoryStream accumulator,
        MemoryStream document)
    {
        if (accumulator is null)
        {
            var first = CreateBoundedMergeOutputStream();
            try
            {
                document.Position = 0;
                document.CopyTo(first);
                first.Position = 0;
                return first;
            }
            catch
            {
                first.Dispose();
                throw;
            }
        }

        var outputStream = CreateBoundedMergeOutputStream();
        try
        {
            using var pdf = new GdPicturePDF();
            accumulator.Position = 0;
            document.Position = 0;
            var mergeResult = pdf.Merge2Documents(accumulator, document, outputStream);
            if (mergeResult != GdPictureStatus.OK)
            {
                logger.LogError("GdPicture incremental merge failed with status: {Status}", mergeResult);
                throw new InvalidOperationException($"Failed to merge documents: {mergeResult}");
            }

            accumulator.Dispose();
            outputStream.Position = 0;
            return outputStream;
        }
        catch
        {
            outputStream.Dispose();
            throw;
        }
    }

    private MemoryStream CreateBoundedMergeOutputStream() =>
        new BoundedMemoryStream(
            conversionOptions.MaxOutputSizeBytes,
            DocumentPdfConversionService.ConversionErrorMessage);

    internal PdfDocumentResponse CreateConversionAwareResponse(
        MemoryStream outputStream,
        List<PageRange> pageRanges)
    {
        var encodedLength = GetBase64EncodedLength(outputStream.Length);
        ValidateBase64ResponseSize(encodedLength, conversionOptions.MaxBase64ResponseSizeBytes);
        return CreateResponse(outputStream, pageRanges);
    }

    private PdfDocumentResponse CreateResponse(
        MemoryStream outputStream,
        List<PageRange> pageRanges)
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

    internal static long GetBase64EncodedLength(long inputBytes) =>
        checked(((inputBytes + 2) / 3) * 4);

    internal static void ValidateBase64ResponseSize(long encodedBytes, long maximumBytes)
    {
        if (encodedBytes > maximumBytes)
        {
            throw new Scv.Core.Exceptions.DocumentSizeLimitExceededException(
                DocumentPdfConversionService.ConversionErrorMessage);
        }
    }

    private async Task RetrieveStreamsInBatches(
        PdfDocumentRequest[] documentRequests,
        CancellationToken cancellationToken,
        Func<int, MemoryStream, Task> consumeStream)
    {
        using var retrievalCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var retrievalToken = retrievalCancellation.Token;
        var batchSize = Math.Min(GetRetrievalBatchSize(), conversionOptions.MaxConcurrentConversions);

        for (var batchStart = 0; batchStart < documentRequests.Length; batchStart += batchSize)
        {
            retrievalToken.ThrowIfCancellationRequested();
            var batchCount = Math.Min(batchSize, documentRequests.Length - batchStart);
            var retrieveTasks = Enumerable.Range(batchStart, batchCount)
                .Select(index => RetrieveStream(index, documentRequests[index], retrievalToken, retrievalCancellation))
                .ToArray();
            (int Index, MemoryStream Stream)[] batchResults;
            try
            {
                batchResults = await Task.WhenAll(retrieveTasks);
            }
            catch
            {
                foreach (var task in retrieveTasks.Where(task => task.IsCompletedSuccessfully))
                {
                    await task.Result.Stream.DisposeAsync();
                }
                throw;
            }

            try
            {
                foreach (var (index, stream) in batchResults.OrderBy(result => result.Index))
                {
                    await consumeStream(index, stream);
                    await stream.DisposeAsync();
                }
            }
            catch
            {
                foreach (var (_, stream) in batchResults)
                {
                    await stream.DisposeAsync();
                }
                throw;
            }
        }
    }

    private int GetRetrievalBatchSize()
    {
        var configuredValue = configuration.GetValue<string>(RetrieveBatchSizeKey);
        return int.TryParse(configuredValue, out var parsedBatchSize) && parsedBatchSize > 0
            ? parsedBatchSize
            : DefaultRetrieveBatchSize;
    }

    internal long CountConvertedBytes(long currentBytes, long streamBytes, Scv.Models.Document.DocumentType documentType)
    {
        if (documentType != Scv.Models.Document.DocumentType.TransitoryDocument)
        {
            return currentBytes;
        }

        if (currentBytes > conversionOptions.MaxAggregateConvertedOutputSizeBytes - streamBytes)
        {
            throw new Scv.Core.Exceptions.DocumentSizeLimitExceededException(
                DocumentPdfConversionService.ConversionErrorMessage);
        }

        return currentBytes + streamBytes;
    }

    private async Task<(int Index, MemoryStream Stream)> RetrieveStream(
        int index,
        PdfDocumentRequest documentRequest,
        CancellationToken cancellationToken,
        CancellationTokenSource retrievalCancellation)
    {
        try
        {
            var stream = await documentRetriever.Retrieve(documentRequest, cancellationToken);
            if (stream is null)
            {
                throw new InvalidOperationException($"Document stream {index} is null");
            }
            return (index, stream);
        }
        catch
        {
            await retrievalCancellation.CancelAsync();
            throw;
        }
    }
}
