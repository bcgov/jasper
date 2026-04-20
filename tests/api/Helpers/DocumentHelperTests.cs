using System;
using Scv.Api.Helpers;
using Xunit;

namespace tests.api.Helpers;

public class DocumentHelperTests
{
    [Fact]
    public void IsPdfOrWordDocumentBase64_ReturnsFalse_WhenNullOrWhitespace()
    {
        Assert.False(DocumentHelper.IsPdfOrWordDocumentBase64(null));
        Assert.False(DocumentHelper.IsPdfOrWordDocumentBase64(""));
        Assert.False(DocumentHelper.IsPdfOrWordDocumentBase64("   "));
    }

    [Fact]
    public void IsPdfOrWordDocumentBase64_ReturnsFalse_WhenNotBase64()
    {
        var result = DocumentHelper.IsPdfOrWordDocumentBase64("not-base64");

        Assert.False(result);
    }

    [Fact]
    public void IsPdfOrWordDocumentBase64_ReturnsTrue_ForPdfSignature()
    {
        var bytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x37 };
        var base64 = Convert.ToBase64String(bytes);

        var result = DocumentHelper.IsPdfOrWordDocumentBase64(base64);

        Assert.True(result);
    }

    [Fact]
    public void IsPdfOrWordDocumentBase64_ReturnsTrue_ForDocSignature()
    {
        var bytes = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00 };
        var base64 = Convert.ToBase64String(bytes);

        var result = DocumentHelper.IsPdfOrWordDocumentBase64(base64);

        Assert.True(result);
    }

    [Fact]
    public void IsPdfOrWordDocumentBase64_ReturnsTrue_ForDocxSignature()
    {
        var bytes = new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0x00 };
        var base64 = Convert.ToBase64String(bytes);

        var result = DocumentHelper.IsPdfOrWordDocumentBase64(base64);

        Assert.True(result);
    }

    [Fact]
    public void IsPdfOrWordDocumentBase64_ReturnsFalse_ForUnsupportedSignature()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };
        var base64 = Convert.ToBase64String(bytes);

        var result = DocumentHelper.IsPdfOrWordDocumentBase64(base64);

        Assert.False(result);
    }
}
