using System.IO;
using System.Text;
using Scv.Api.Documents.Conversion;
using Xunit;

namespace tests.api.Documents.Conversion;

public class PdfFileValidationTests
{
    [Fact]
    public void HasPdfSignature_ReturnsTrueForPdfHeaderAndPreservesPosition()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF-1.7\ncontent"));
        stream.Position = 4;

        var result = PdfFileValidation.HasPdfSignature(stream);

        Assert.True(result);
        Assert.Equal(4, stream.Position);
    }

    [Fact]
    public void HasPdfSignature_ReturnsFalseForInvalidHeaderAndPreservesPosition()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("not-a-pdf"));
        stream.Position = 3;

        var result = PdfFileValidation.HasPdfSignature(stream);

        Assert.False(result);
        Assert.Equal(3, stream.Position);
    }

    [Fact]
    public void HasPdfSignature_ReturnsFalseForShortStream()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("%PDF"));

        Assert.False(PdfFileValidation.HasPdfSignature(stream));
    }
}
