using Bogus;
using Scv.Core.Helpers.Extensions;
using Xunit;

namespace tests.core.Helpers.Extensions;

public class LoggingExtensionsTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void SanitizeForLog_WithNull_ReturnsEmptyString()
    {
        string value = null;

        var result = value.SanitizeForLog();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeForLog_WithEmptyString_ReturnsEmptyString()
    {
        var result = string.Empty.SanitizeForLog();

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void SanitizeForLog_WithoutLineBreaks_ReturnsSameValue()
    {
        var value = _faker.Lorem.Sentence();

        var result = value.SanitizeForLog();

        Assert.Equal(value, result);
    }

    [Fact]
    public void SanitizeForLog_RemovesCarriageReturn()
    {
        var result = "abc\rdef".SanitizeForLog();

        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void SanitizeForLog_RemovesLineFeed()
    {
        var result = "abc\ndef".SanitizeForLog();

        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void SanitizeForLog_RemovesCarriageReturnLineFeed()
    {
        var result = "abc\r\ndef".SanitizeForLog();

        Assert.Equal("abcdef", result);
    }

    [Fact]
    public void SanitizeForLog_RemovesMultipleLineBreaks()
    {
        var result = "line1\r\nline2\nline3\rline4".SanitizeForLog();

        Assert.Equal("line1line2line3line4", result);
    }

    [Fact]
    public void SanitizeForLog_PreventsForgedLogEntry()
    {
        // Attacker attempts to inject a fake log line via CRLF.
        var malicious = "userId=42\r\nINFO: Admin access granted";

        var result = malicious.SanitizeForLog();

        Assert.Equal("userId=42INFO: Admin access granted", result);
        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
    }

    [Theory]
    [InlineData("\r", "")]
    [InlineData("\n", "")]
    [InlineData("\r\n", "")]
    [InlineData("no-breaks", "no-breaks")]
    [InlineData("a\rb\nc", "abc")]
    public void SanitizeForLog_ReturnsExpected(string input, string expected)
    {
        Assert.Equal(expected, input.SanitizeForLog());
    }

    [Fact]
    public void MaskEmailForLog_WithNull_ReturnsMask()
    {
        string value = null;

        var result = value.MaskEmailForLog();

        Assert.Equal("***", result);
    }

    [Fact]
    public void MaskEmailForLog_WithEmptyString_ReturnsMask()
    {
        var result = string.Empty.MaskEmailForLog();

        Assert.Equal("***", result);
    }

    [Fact]
    public void MaskEmailForLog_WithWhitespace_ReturnsMask()
    {
        var result = "   ".MaskEmailForLog();

        Assert.Equal("***", result);
    }

    [Fact]
    public void MaskEmailForLog_MasksLocalPartAndKeepsDomain()
    {
        var result = "john.doe@example.com".MaskEmailForLog();

        Assert.Equal("j***@example.com", result);
    }

    [Fact]
    public void MaskEmailForLog_DoesNotExposeFullLocalPart()
    {
        var result = "john.doe@example.com".MaskEmailForLog();

        Assert.DoesNotContain("john.doe", result);
    }

    [Fact]
    public void MaskEmailForLog_SanitizesLineBreaks()
    {
        // Attacker attempts to forge a log line via CRLF within the email value.
        var result = "john\r\ndoe@example.com".MaskEmailForLog();

        Assert.DoesNotContain("\r", result);
        Assert.DoesNotContain("\n", result);
        Assert.Equal("j***@example.com", result);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("john.doe@")]
    [InlineData("@")]
    public void MaskEmailForLog_WithInvalidEmailShape_ReturnsMask(string input)
    {
        Assert.Equal("***", input.MaskEmailForLog());
    }

    [Theory]
    [InlineData("a@b.com", "a***@b.com")]
    [InlineData("John.Doe@Example.COM", "J***@Example.COM")]
    [InlineData("user+tag@sub.domain.org", "u***@sub.domain.org")]
    public void MaskEmailForLog_ReturnsExpected(string input, string expected)
    {
        Assert.Equal(expected, input.MaskEmailForLog());
    }
}
