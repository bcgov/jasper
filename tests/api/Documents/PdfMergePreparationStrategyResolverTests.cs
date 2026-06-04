using System;
using Moq;
using Scv.Api.Documents;
using Scv.Models.Document;
using Xunit;

namespace tests.api.Documents;

public class PdfMergePreparationStrategyResolverTests
{
    [Fact]
    public void Resolve_ReturnsNone_ForTransitoryDocuments()
    {
        var resolver = CreateResolver();

        var mode = resolver.Resolve(new PdfDocumentRequest
        {
            Type = DocumentType.TransitoryDocument,
            Data = new PdfDocumentRequestDetails { Path = "path/to/document.pdf" }
        });

        Assert.Equal(PdfMergePreparationMode.None, mode);
    }

    [Fact]
    public void Resolve_ReturnsFlatten_ForNonTransitoryDocuments()
    {
        var resolver = CreateResolver();

        var mode = resolver.Resolve(new PdfDocumentRequest
        {
            Type = DocumentType.File,
            Data = new PdfDocumentRequestDetails { DocumentId = "doc-id", FileId = "file-id" }
        });

        Assert.Equal(PdfMergePreparationMode.Flatten, mode);
    }

    [Fact]
    public void Resolve_Throws_WhenNoStrategyMatches()
    {
        var resolver = new PdfMergePreparationStrategyResolver([]);

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(new PdfDocumentRequest
        {
            Type = DocumentType.File,
            Data = new PdfDocumentRequestDetails { DocumentId = "doc-id" }
        }));
    }

    [Fact]
    public void Resolve_Throws_WhenMultipleStrategiesMatch()
    {
        var overlappingStrategyMock = new Mock<IPdfMergePreparationStrategy>();
        overlappingStrategyMock
            .SetupGet(s => s.PreparationMode)
            .Returns(PdfMergePreparationMode.Flatten);
        overlappingStrategyMock
            .Setup(s => s.CanHandle(It.IsAny<PdfDocumentRequest>()))
            .Returns<PdfDocumentRequest>(documentRequest => documentRequest?.Type == DocumentType.File);

        var overlappingStrategies = new IPdfMergePreparationStrategy[]
        {
            new DefaultPdfMergePreparationStrategy(),
            overlappingStrategyMock.Object,
        };
        var resolver = new PdfMergePreparationStrategyResolver(overlappingStrategies);

        Assert.Throws<InvalidOperationException>(() => resolver.Resolve(new PdfDocumentRequest
        {
            Type = DocumentType.File,
            Data = new PdfDocumentRequestDetails { DocumentId = "doc-id" }
        }));
    }

    private static PdfMergePreparationStrategyResolver CreateResolver()
    {
        return new PdfMergePreparationStrategyResolver(
        [
            new TransitoryDocumentPdfMergePreparationStrategy(),
            new DefaultPdfMergePreparationStrategy(),
        ]);
    }
}