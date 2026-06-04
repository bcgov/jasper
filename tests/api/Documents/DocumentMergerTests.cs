using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GdPicture14;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Documents.Strategies;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Document;
using tests.api.Services;
using Xunit;

using ScvDocumentType = Scv.Models.Document.DocumentType;

namespace tests.api.Documents;

public class DocumentMergerTest : ServiceTestBase
{
    [Fact]
    public async Task MergeDocuments_RetrievesDocumentsInBatches()
    {
        var retrieverMock = new Mock<IDocumentRetriever>();
        var preparationStrategyResolverMock = new Mock<IPdfMergePreparationStrategyResolver>();
        var loggerMock = new Mock<ILogger<DocumentMerger>>();
        var configuration = new ConfigurationBuilder().Build();
        var merger = new DocumentMerger(retrieverMock.Object, preparationStrategyResolverMock.Object, loggerMock.Object, configuration);

        var activeRequests = 0;
        var maxConcurrentRequests = 0;

        retrieverMock
            .Setup(x => x.Retrieve(It.IsAny<PdfDocumentRequest>()))
            .Returns(async () =>
            {
                var current = Interlocked.Increment(ref activeRequests);
                UpdateMaxConcurrentRequests(ref maxConcurrentRequests, current);

                await Task.Delay(20);

                Interlocked.Decrement(ref activeRequests);
                return null;
            });

        var requests = Enumerable.Range(1, 25)
            .Select(index => new PdfDocumentRequest
            {
                Type = ScvDocumentType.File,
                Data = new PdfDocumentRequestDetails
                {
                    DocumentId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes($"doc{index}")),
                    FileId = $"file{index}",
                    CorrelationId = $"corr{index}"
                }
            })
            .ToArray();

        await Assert.ThrowsAsync<InvalidOperationException>(() => merger.MergeDocuments(requests));

        Assert.True(maxConcurrentRequests <= 10, $"Expected at most 10 concurrent requests but saw {maxConcurrentRequests}.");
    }

    [Fact]
    public async Task MergeDocuments_ThrowsInvalidOperationException_WhenStreamUnreadable()
    {
        var retrieverMock = new Mock<IDocumentRetriever>();
        var preparationStrategyResolverMock = new Mock<IPdfMergePreparationStrategyResolver>();
        var loggerMock = new Mock<ILogger<DocumentMerger>>();
        var configuration = new ConfigurationBuilder().Build();
        var merger = new DocumentMerger(retrieverMock.Object, preparationStrategyResolverMock.Object, loggerMock.Object, configuration);
        var docId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes("doc1"));
        var request = new PdfDocumentRequest
        {
            Data = new PdfDocumentRequestDetails
            {
                DocumentId = docId,
                FileId = "file1",
                CorrelationId = "corr1"
            }
        };

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await merger.MergeDocuments([request]);
        });
    }

    [Fact]
    public void PreparePdfForMerge_ThrowsForUnsupportedPreparationMode()
    {
        var retrieverMock = new Mock<IDocumentRetriever>();
        var preparationStrategyResolverMock = new Mock<IPdfMergePreparationStrategyResolver>();
        var loggerMock = new Mock<ILogger<DocumentMerger>>();
        var configuration = new ConfigurationBuilder().Build();
        var merger = new DocumentMerger(retrieverMock.Object, preparationStrategyResolverMock.Object, loggerMock.Object, configuration);

        preparationStrategyResolverMock
            .Setup(x => x.Resolve(It.IsAny<PdfDocumentRequest>()))
            .Returns((PdfMergePreparationMode)999);

        var request = new PdfDocumentRequest
        {
            Type = ScvDocumentType.TransitoryDocument,
            Data = new PdfDocumentRequestDetails { Path = "path/to/document.pdf" }
        };

        using var pdf = new GdPicturePDF();
        var method = typeof(DocumentMerger).GetMethod("PreparePdfForMerge", BindingFlags.Instance | BindingFlags.NonPublic);

        var exception = Assert.Throws<TargetInvocationException>(() => method!.Invoke(merger, [pdf, request, 0]));

        Assert.IsType<InvalidOperationException>(exception.InnerException);
        Assert.Equal("Unsupported PDF merge preparation mode: 999", exception.InnerException!.Message);
    }

    [Fact]
    public async Task MergeDocuments_PreservesBookmarks_ForTransitoryDocuments()
    {
        var transitoryDocumentsServiceMock = new Mock<ITransitoryDocumentsService>();
        var transitoryStrategyLoggerMock = new Mock<ILogger<TransitoryDocumentStrategy>>();
        var documentRetrieverLoggerMock = new Mock<ILogger<DocumentRetriever>>();
        var documentMergerLoggerMock = new Mock<ILogger<DocumentMerger>>();
        var configuration = new ConfigurationBuilder().Build();
        const string documentPath = "path/to/document.pdf";
        const string bookmarkTitle = "Transitory Bookmark";

        transitoryDocumentsServiceMock
            .Setup(x => x.DownloadFile(documentPath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new FileStreamResponse(
                CreatePdfWithBookmark(bookmarkTitle, 2),
                "transitory.pdf",
                "application/pdf"));

        var documentRetriever = new DocumentRetriever(
            [new TransitoryDocumentStrategy(transitoryDocumentsServiceMock.Object, transitoryStrategyLoggerMock.Object)],
            documentRetrieverLoggerMock.Object);
        var preparationStrategyResolver = new PdfMergePreparationStrategyResolver(
            [new DefaultPdfMergePreparationStrategy(), new TransitoryDocumentPdfMergePreparationStrategy()]);
        var merger = new DocumentMerger(documentRetriever, preparationStrategyResolver, documentMergerLoggerMock.Object, configuration);

        var response = await merger.MergeDocuments([
            new PdfDocumentRequest
            {
                Type = ScvDocumentType.TransitoryDocument,
                Data = new PdfDocumentRequestDetails
                {
                    Path = documentPath,
                }
            }
        ]);

        using var mergedStream = new MemoryStream(Convert.FromBase64String(response.Base64Pdf));
        using var mergedPdf = new GdPicturePDF();
        Assert.Equal(GdPictureStatus.OK, mergedPdf.LoadFromStream(mergedStream, true));
        Assert.Equal(2, mergedPdf.GetPageCount());
        Assert.Single(response.PageRanges);
        Assert.Equal(0, response.PageRanges[0].Start);
        Assert.Equal(2, response.PageRanges[0].End);
        Assert.Equal(1, mergedPdf.GetBookmarkCount());

        var bookmarkId = mergedPdf.GetBookmarkRootID();
        Assert.True(bookmarkId > 0);
        Assert.Equal(bookmarkTitle, mergedPdf.GetBookmarkTitle(bookmarkId));

        var actionId = mergedPdf.GetBookmarkActionID(bookmarkId);
        Assert.True(actionId > 0);

        var destinationType = PdfDestinationType.DestinationTypeXYZ;
        var destinationPage = 0;
        var left = 0f;
        var bottom = 0f;
        var right = 0f;
        var top = 0f;
        var zoom = 0f;

        Assert.Equal(
            GdPictureStatus.OK,
            mergedPdf.GetActionPageDestination(
                actionId,
                ref destinationType,
                ref destinationPage,
                ref left,
                ref bottom,
                ref right,
                ref top,
                ref zoom));
        Assert.Equal(2, destinationPage);
    }

    private static void UpdateMaxConcurrentRequests(ref int maxConcurrentRequests, int current)
    {
        int observedMax;

        do
        {
            observedMax = maxConcurrentRequests;
            if (current <= observedMax)
                return;
        }
        while (Interlocked.CompareExchange(ref maxConcurrentRequests, current, observedMax) != observedMax);
    }

    private static MemoryStream CreatePdfWithBookmark(string bookmarkTitle, int destinationPage)
    {
        using var pdf = new GdPicturePDF();
        Assert.Equal(GdPictureStatus.OK, pdf.NewPDF());
        Assert.Equal(GdPictureStatus.OK, pdf.NewPage(8.5f, 11f));
        Assert.Equal(GdPictureStatus.OK, pdf.NewPage(8.5f, 11f));

        var bookmarkId = pdf.NewBookmark(0, bookmarkTitle);
        Assert.True(bookmarkId > 0);

        var actionId = pdf.NewActionGoTo(PdfDestinationType.DestinationTypeXYZ, destinationPage, 0, 0, 0, 0, 1);
        Assert.True(actionId > 0);
        Assert.Equal(GdPictureStatus.OK, pdf.SetBookmarkAction(bookmarkId, actionId));

        var stream = new MemoryStream();
        Assert.Equal(GdPictureStatus.OK, pdf.SaveToStream(stream));
        stream.Position = 0;
        return stream;
    }
}
