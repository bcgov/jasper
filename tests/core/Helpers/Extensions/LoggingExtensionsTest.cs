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
}
