using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Documents;
using Scv.Models.Document;
using tests.api.Services;
using Xunit;

namespace tests.api.Documents;

public class DocumentMergerTest : ServiceTestBase
{
    [Fact]
    public async Task MergeDocuments_RetrievesDocumentsInBatches()
    {
        var retrieverMock = new Mock<IDocumentRetriever>();
        var loggerMock = new Mock<ILogger<DocumentMerger>>();
        var configuration = new ConfigurationBuilder().Build();
        var merger = new DocumentMerger(retrieverMock.Object, loggerMock.Object, configuration);

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
                Type = DocumentType.File,
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
        var loggerMock = new Mock<ILogger<DocumentMerger>>();
        var configuration = new ConfigurationBuilder().Build();
        var merger = new DocumentMerger(retrieverMock.Object, loggerMock.Object, configuration);
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
}
