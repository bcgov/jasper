using System;
using System.IO;
using System.Text;
using Bogus;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Scv.Api.Documents.Extractors;
using Xunit;

namespace tests.api.Documents.Extractors;

public class DeskOrderDetailsExtractorTests
{
    private readonly DeskOrderDetailsExtractor _extractor = new();
    private readonly Faker _faker = new();

    [Fact]
    public void Extract_ThrowsArgumentNullException_WhenStreamIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => _extractor.Extract(null));
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenStreamIsNotWordDocument()
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("this is plain text, not a docx"));

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Stream is not a valid Word (.docx) document.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenDirectionsLabelIsMissing()
    {
        var term = _faker.Lorem.Sentence();
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(term));
            body.AppendChild(SignatureSdt());
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Unable to extract directions from the document body.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ReturnsDirectionsAndOrderTerms_WhenDocumentIsValid()
    {
        var directionsText = _faker.Lorem.Sentence();
        var term1 = _faker.Lorem.Sentence();
        var term2 = _faker.Lorem.Sentence();

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(directionsText));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(term1));
            body.AppendChild(ParagraphOf(term2));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Equal(directionsText, result.Directions);
        Assert.Equal(2, result.OrderTerms.Length);

        Assert.Equal(term1, result.OrderTerms[0].Text);
        Assert.Equal(1, result.OrderTerms[0].SequenceNumber);
        Assert.Equal(1, result.OrderTerms[0].DisplaySortNumber);

        Assert.Equal(term2, result.OrderTerms[1].Text);
        Assert.Equal(2, result.OrderTerms[1].SequenceNumber);
        Assert.Equal(2, result.OrderTerms[1].DisplaySortNumber);
    }

    [Fact]
    public void Extract_TrimsWhitespaceFromOrderTermText()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf("   Padded term text   "));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Single(result.OrderTerms);
        Assert.Equal("Padded term text", result.OrderTerms[0].Text);
    }

    [Fact]
    public void Extract_SkipsEmptyAndWhitespaceParagraphs_BetweenOrderTermsLabelAndSignature()
    {
        var realTerm = _faker.Lorem.Sentence();
        var fakeDirection = _faker.Lorem.Sentence();

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(fakeDirection));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(string.Empty));
            body.AppendChild(ParagraphOf("   "));
            body.AppendChild(ParagraphOf(realTerm));
            body.AppendChild(ParagraphOf(string.Empty));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Single(result.OrderTerms);
        Assert.Equal(realTerm, result.OrderTerms[0].Text);
    }

    [Fact]
    public void Extract_IncludesSdtContent_WhenSdtIsFilled()
    {
        var sdtContent = _faker.Lorem.Sentence();

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(FilledSdtBlock(sdtContent));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Single(result.OrderTerms);
        Assert.Equal(sdtContent, result.OrderTerms[0].Text);
    }

    [Fact]
    public void Extract_IgnoresSdtContent_WhenSdtIsShowingPlaceholder()
    {
        var realTerm = _faker.Lorem.Sentence();

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(PlaceholderSdtBlock("[placeholder content that should be ignored]"));
            body.AppendChild(ParagraphOf(realTerm));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Single(result.OrderTerms);
        Assert.Equal(realTerm, result.OrderTerms[0].Text);
    }

    [Fact]
    public void Extract_DetectsSignature_FromSdtTagOnDescendantOfBodyChild()
    {
        // Wraps an SdtRun (with the signature tag) inside a paragraph so the SdtElement
        // is a descendant, not the body child itself. This exercises the Descendants<SdtElement>()
        // branch of HasSignatureTag.
        var term = _faker.Lorem.Sentence();

        var paragraphWithDescendantSignature = new Paragraph(
            new SdtRun(
                new SdtProperties(new Tag { Val = "Insert Signature here" }),
                new SdtContentRun(new Run(new Text("name")))));

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(term));
            body.AppendChild(paragraphWithDescendantSignature);
        });

        var result = _extractor.Extract(stream);

        Assert.Single(result.OrderTerms);
        Assert.Equal(term, result.OrderTerms[0].Text);
    }

    [Fact]
    public void Extract_DetectsSignature_FromInnerText_CaseInsensitive()
    {
        var term = _faker.Lorem.Sentence();

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(term));
            body.AppendChild(ParagraphOf("insert SIGNATURE"));
        });

        var result = _extractor.Extract(stream);

        Assert.Single(result.OrderTerms);
        Assert.Equal(term, result.OrderTerms[0].Text);
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenOrderTermsLabelMissing()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf("Some unrelated content"));
            body.AppendChild(SignatureSdt());
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Unable to extract directions from the document body.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenSignatureMissing()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            // No signature element follows the order terms label.
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Unable to extract order terms from the document body.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static MemoryStream BuildDocxStream(Action<Body> configureBody)
    {
        var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document();
            var body = mainPart.Document.AppendChild(new Body());
            configureBody(body);
        }
        stream.Position = 0;
        return stream;
    }

    private static Paragraph ParagraphOf(string text) =>
        new(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }));

    private static SdtBlock FilledSdtBlock(string text) =>
        new(
            new SdtProperties(),
            new SdtContentBlock(
                new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }))));

    private static SdtBlock PlaceholderSdtBlock(string text) =>
        new(
            new SdtProperties(new ShowingPlaceholder()),
            new SdtContentBlock(
                new Paragraph(new Run(new Text(text) { Space = SpaceProcessingModeValues.Preserve }))));

    private static SdtBlock SignatureSdt(string tagValue = "Insert Signature") =>
        new(
            new SdtProperties(new Tag { Val = tagValue }),
            new SdtContentBlock(new Paragraph(new Run(new Text("[signature]")))));
}
