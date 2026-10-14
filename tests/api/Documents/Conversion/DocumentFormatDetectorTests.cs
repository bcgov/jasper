using System.IO;
using System.IO.Compression;
using System.Text;
using Scv.Api.Documents.Conversion;
using Xunit;

namespace tests.api.Documents.Conversion;

public class DocumentFormatDetectorTests
{
    [Fact]
    public void Detect_UsesRootOfficeDocumentRelationship_WhenPackageContainsMultipleKnownContentTypes()
    {
        using var package = CreatePackage(
            "word/document.xml",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
              <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml" />
            </Types>
            """);

        var format = DocumentFormatDetector.Detect(package);

        Assert.Equal(Scv.Api.Documents.Conversion.DocumentFormat.Docx, format);
    }

    [Fact]
    public void Detect_ReturnsUnknown_WhenPrimaryPartHasNoMatchingContentTypeOverride()
    {
        using var package = CreatePackage(
            "word/document.xml",
            """
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml" />
            </Types>
            """);

        var format = DocumentFormatDetector.Detect(package);

        Assert.Equal(Scv.Api.Documents.Conversion.DocumentFormat.Unknown, format);
    }

    private static MemoryStream CreatePackage(string primaryPart, string contentTypes)
    {
        var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, "[Content_Types].xml", contentTypes);
            WriteEntry(
                archive,
                "_rels/.rels",
                $$"""
                <?xml version="1.0" encoding="UTF-8"?>
                <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
                  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="{{primaryPart}}" />
                </Relationships>
                """);
            WriteEntry(archive, primaryPart, "<document />");
        }

        stream.Position = 0;
        return stream;
    }

    private static void WriteEntry(ZipArchive archive, string name, string content)
    {
        var entry = archive.CreateEntry(name);
        using var writer = new StreamWriter(entry.Open(), Encoding.UTF8);
        writer.Write(content);
    }
}
