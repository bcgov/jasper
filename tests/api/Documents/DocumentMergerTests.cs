using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.WebUtilities;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Models.Document;
using Xunit;
using tests.api.Services;

namespace tests.api.Documents;

public class DocumentMergerTest : ServiceTestBase
{
    [Fact]
    public async Task MergeDocuments_ThrowsInvalidOperationException_WhenStreamUnreadable()
    {
        var retrieverMock = new Mock<IDocumentRetriever>();
        var merger = new DocumentMerger(retrieverMock.Object);
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
}