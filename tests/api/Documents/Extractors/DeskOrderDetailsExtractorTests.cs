using System;
using System.IO;
using System.Text;
using Bogus;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Scv.Api.Documents.Extractors;
using Xunit;
// Alias for the Office 2010 Word namespace (w14 XML prefix); disambiguates checkbox types.
using W14 = DocumentFormat.OpenXml.Office2010.Word;

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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(term));
            body.AppendChild(SignatureSdt());
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Unable to extract desk order details (reasons for rejection, directions or order terms) from the document body.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ReturnsDirectionsAndOrderTerms_WhenDocumentIsValid()
    {
        var rejectionReasons = _faker.Lorem.Sentence();
        var directionsText = _faker.Lorem.Sentence();
        var term1 = _faker.Lorem.Sentence();
        var term2 = _faker.Lorem.Sentence();

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(rejectionReasons));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(directionsText));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(term1));
            body.AppendChild(ParagraphOf(term2));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Equal(rejectionReasons, result.ReasonsForRejection);
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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
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
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Unable to extract desk order details (reasons for rejection, directions or order terms) from the document body.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenSignatureMissing()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            // No signature element follows the order terms label.
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Unable to extract order terms from the document body.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Extract_SetsIsClerkToSign_AccordingToCheckboxState(bool isChecked)
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
            body.AppendChild(ClerkApprovalParagraph(isChecked));
        });

        var result = _extractor.Extract(stream);

        Assert.Equal(isChecked, result.IsClerkToSign);
    }

    [Fact]
    public void Extract_SetsIsClerkToSign_WhenCheckboxCheckedUsingOneValue()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
            body.AppendChild(ClerkApprovalParagraph(W14.OnOffValues.One));
        });

        var result = _extractor.Extract(stream);

        Assert.True(result.IsClerkToSign);
    }

    [Fact]
    public void Extract_SetsIsClerkToSignFalse_WhenClerkApprovalLabelIsMissing()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
            // No clerk approval paragraph present.
        });

        var result = _extractor.Extract(stream);

        Assert.False(result.IsClerkToSign);
    }

    [Fact]
    public void Extract_SetsIsClerkToSignFalse_WhenClerkApprovalLabelPresentButNoCheckbox()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.CLERK_APPROVAL_LABEL));
        });

        var result = _extractor.Extract(stream);

        Assert.False(result.IsClerkToSign);
    }

    [Fact]
    public void Extract_ReturnsReasonsForRejection_WhenDocumentIsValid()
    {
        var rejectionReason1 = _faker.Lorem.Sentence();
        var rejectionReason2 = _faker.Lorem.Sentence();

        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(rejectionReason1));
            body.AppendChild(ParagraphOf(rejectionReason2));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Equal($"{rejectionReason1} {rejectionReason2}", result.ReasonsForRejection);
    }

    [Fact]
    public void Extract_ReturnsEmptyReasonsForRejection_WhenNoContentBetweenLabels()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
        });

        var result = _extractor.Extract(stream);

        Assert.Equal(string.Empty, result.ReasonsForRejection);
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenRejectionReasonsLabelIsMissing()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Unable to extract desk order details (reasons for rejection, directions or order terms) from the document body.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenDirectionsContentIsEmpty()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(SignatureSdt());
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Directions content is empty or whitespace.", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Extract_ThrowsInvalidDataException_WhenOrderTermsContentIsEmpty()
    {
        using var stream = BuildDocxStream(body =>
        {
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.REJECTION_REASONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.DIRECTIONS_LABEL));
            body.AppendChild(ParagraphOf(_faker.Lorem.Sentence()));
            body.AppendChild(ParagraphOf(DeskOrderDetailsExtractor.ORDER_TERMS_LABEL));
            body.AppendChild(SignatureSdt());
        });

        var ex = Assert.Throws<InvalidDataException>(() => _extractor.Extract(stream));
        Assert.Contains("Order terms content is empty or whitespace.", ex.Message, StringComparison.OrdinalIgnoreCase);
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

    private static Paragraph ClerkApprovalParagraph(bool isChecked) =>
        ClerkApprovalParagraph(isChecked ? W14.OnOffValues.True : W14.OnOffValues.False);

    private static Paragraph ClerkApprovalParagraph(W14.OnOffValues checkedValue) =>
        new(
            new Run(new Text(DeskOrderDetailsExtractor.CLERK_APPROVAL_LABEL) { Space = SpaceProcessingModeValues.Preserve }),
            new SdtRun(
                new SdtProperties(
                    new W14.SdtContentCheckBox(new W14.Checked { Val = checkedValue })),
                new SdtContentRun(new Run(new Text("[checkbox]")))));
}
