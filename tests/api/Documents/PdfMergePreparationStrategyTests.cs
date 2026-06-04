using Scv.Api.Documents;
using Scv.Models.Document;
using Xunit;

namespace tests.api.Documents;

public class PdfMergePreparationStrategyTests
{
    [Fact]
    public void DefaultStrategy_ReturnsFlatten_AndSkipsTransitoryDocuments()
    {
        var strategy = new DefaultPdfMergePreparationStrategy();

        var fileRequest = new PdfDocumentRequest { Type = DocumentType.File };
        var transitoryRequest = new PdfDocumentRequest { Type = DocumentType.TransitoryDocument };

        Assert.Equal(PdfMergePreparationMode.Flatten, strategy.PreparationMode);
        Assert.True(strategy.CanHandle(fileRequest));
        Assert.False(strategy.CanHandle(transitoryRequest));
    }

    [Fact]
    public void TransitoryStrategy_ReturnsNone_AndOnlyHandlesTransitoryDocuments()
    {
        var strategy = new TransitoryDocumentPdfMergePreparationStrategy();

        var transitoryRequest = new PdfDocumentRequest { Type = DocumentType.TransitoryDocument };
        var fileRequest = new PdfDocumentRequest { Type = DocumentType.File };

        Assert.Equal(PdfMergePreparationMode.None, strategy.PreparationMode);
        Assert.True(strategy.CanHandle(transitoryRequest));
        Assert.False(strategy.CanHandle(fileRequest));
    }
}