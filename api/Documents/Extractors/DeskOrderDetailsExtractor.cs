using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Scv.Core.Helpers;
using Scv.Models.Order;

namespace Scv.Api.Documents.Extractors;

public class DeskOrderDetailsExtractor : IDeskOrderDetailsExtractor
{
    public const string DIRECTIONS_LABEL = "Directions:";
    public const string ORDER_TERMS_LABEL = "Terms of Order:";
    public const string SIGNATURE_TAG = "Insert Signature";

    public DeskOrderDetailsDto Extract(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        if (!DocumentHelper.IsWordDocument(stream))
        {
            throw new InvalidDataException("Stream is not a valid Word (.docx) document.");
        }

        using var wordDoc = WordprocessingDocument.Open(stream, false);

        var body = (wordDoc.MainDocumentPart?.Document?.Body)
            ?? throw new InvalidDataException("Unable to extract desk order details from the document body.");

        // Get the indices of the "Directions:", "Terms of Order:" section and
        // the signature section to be able to extract the relevant content for each section
        var (directionsIndex, orderTermsIndex, signatureIndex) = GetSectionIndices(body);

        if (directionsIndex == -1 || orderTermsIndex == -1 || directionsIndex >= orderTermsIndex)
        {
            throw new InvalidDataException("Unable to extract directions from the document body.");
        }

        if (signatureIndex == -1 || orderTermsIndex >= signatureIndex)
        {
            throw new InvalidDataException("Unable to extract order terms from the document body.");
        }

        // Whatever is between "Directions:" and "Terms of Order:" is the Directions content.
        var directionsContent = ExtractContentBetween(body, directionsIndex, orderTermsIndex);

        if (directionsContent == null
            || directionsContent.Count == 0
            || string.IsNullOrWhiteSpace(directionsContent[0]))
        {
            throw new InvalidDataException("Directions content is empty or whitespace.");
        }

        // Whatever is between "Terms of Order:" and the signature field are the Order Terms.
        var orderTermsContent = ExtractContentBetween(body, orderTermsIndex, signatureIndex);

        if (orderTermsContent == null
            || orderTermsContent.Count == 0
            || string.IsNullOrWhiteSpace(orderTermsContent[0]))
        {
            throw new InvalidDataException("Order terms content is empty or whitespace.");
        }

        return new DeskOrderDetailsDto
        {
            Directions = string.Join(" ", directionsContent),
            OrderTerms = [.. orderTermsContent
                .Select((text, index) => new OrderTermDto
                {
                    SequenceNumber = index + 1,
                    DisplaySortNumber = index + 1,
                    Text = text
                })]
        };
    }

    private static List<string> ExtractContentBetween(Body body, int startIndex, int endIndex)
    {
        var contents = new List<string>();

        for (int i = startIndex + 1; i < endIndex; i++)
        {
            var element = body.ChildElements[i];

            switch (element)
            {
                // Get the text if Sdt is filled
                case SdtElement sdt when !IsPlaceholderShowing(sdt):
                    AddIfNotEmpty(contents, sdt.InnerText);
                    break;

                // Ignore Sdt that is not filled
                case SdtElement:
                    break;

                // Try to capture any other element (paragraph, table, etc.) if it has visible text
                default:
                    AddIfNotEmpty(contents, element.InnerText);
                    break;
            }
        }

        return contents;
    }

    private static void AddIfNotEmpty(List<string> content, string text)
    {
        var trimmed = text?.Trim();
        if (!string.IsNullOrEmpty(trimmed))
        {
            content.Add(trimmed);
        }
    }

    private static bool IsPlaceholderShowing(SdtElement sdt) =>
        sdt.SdtProperties?.GetFirstChild<ShowingPlaceholder>() is not null;

    private static (int directionsIndex, int orderTermsIndex, int signatureIndex) GetSectionIndices(Body body)
    {
        var directionsIndex = -1;
        var orderTermsIndex = -1;
        var signatureIndex = -1;

        for (int i = 0; i < body.ChildElements.Count; i++)
        {
            var child = body.ChildElements[i];

            if (directionsIndex < 0 && IsLabelMatch(child.InnerText, DIRECTIONS_LABEL))
            {
                directionsIndex = i;
            }
            else if (directionsIndex >= 0 && orderTermsIndex < 0 && IsLabelMatch(child.InnerText, ORDER_TERMS_LABEL))
            {
                orderTermsIndex = i;
            }
            else if (orderTermsIndex >= 0 && HasSignatureTag(child))
            {
                signatureIndex = i;
                break;
            }
        }

        return (directionsIndex, orderTermsIndex, signatureIndex);
    }

    private static bool IsLabelMatch(string text, string label) =>
        string.Equals(text?.Trim(), label, StringComparison.OrdinalIgnoreCase);

    private static bool HasSignatureTag(OpenXmlElement element)
    {
        // Check if the element or any of its descendants is a content control with a tag that contains the signature tag.
        var sdts = element is SdtElement self
            ? element.Descendants<SdtElement>().Prepend(self)
            : element.Descendants<SdtElement>();

        if (sdts.Any(TagMatches))
        {
            return true;
        }

        return element.InnerText.Contains(SIGNATURE_TAG, StringComparison.OrdinalIgnoreCase);
    }

    private static bool TagMatches(SdtElement s) =>
        s.SdtProperties?
            .GetFirstChild<Tag>()?
            .Val?.Value?
            .Contains(SIGNATURE_TAG, StringComparison.OrdinalIgnoreCase) == true;
}
