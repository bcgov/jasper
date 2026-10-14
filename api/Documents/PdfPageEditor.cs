using System;
using System.IO;
using GdPicture14;

namespace Scv.Api.Documents;

public class PdfPageEditor : IPdfPageEditor
{
    public MemoryStream AppendPages(MemoryStream document, MemoryStream pages)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(pages);

        document.Position = 0;
        pages.Position = 0;

        var merged = new MemoryStream();
        using var converter = new GdPictureDocumentConverter();
        var status = converter.CombineToPDF([document, pages], merged, PdfConformance.PDF);
        if (status != GdPictureStatus.OK)
        {
            merged.Dispose();
            throw new InvalidOperationException($"Failed to append pages to the document: {status}");
        }

        merged.Position = 0;
        return merged;
    }

    public MemoryStream RemoveLastPage(MemoryStream document)
    {
        ArgumentNullException.ThrowIfNull(document);

        document.Position = 0;

        using var pdf = new GdPicturePDF();
        var loadStatus = pdf.LoadFromStream(document, false);
        if (loadStatus != GdPictureStatus.OK)
        {
            throw new InvalidOperationException($"Failed to load the document: {loadStatus}");
        }

        var pageCount = pdf.GetPageCount();
        if (pageCount < 2)
        {
            return document;
        }

        var deleteStatus = pdf.DeletePage(pageCount);
        if (deleteStatus != GdPictureStatus.OK)
        {
            throw new InvalidOperationException($"Failed to remove the last page: {deleteStatus}");
        }

        var output = new MemoryStream();
        var saveStatus = pdf.SaveToStream(output);
        if (saveStatus != GdPictureStatus.OK)
        {
            output.Dispose();
            throw new InvalidOperationException($"Failed to save the document: {saveStatus}");
        }

        output.Position = 0;
        return output;
    }
}
