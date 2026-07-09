using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class CsoTextSanitizerTests
{
    private readonly CsoTextSanitizer _sanitizer = new();

    [Theory]
    [InlineData(null, "")]
    [InlineData("", "")]
    [InlineData("   ", "")]
    public void Sanitize_ReturnsEmptyString_WhenInputIsNullEmptyOrWhitespace(string input, string expected)
    {
        Assert.Equal(expected, _sanitizer.Sanitize(input));
    }

    [Fact]
    public void Sanitize_LeavesRegularAsciiTextUnchanged()
    {
        const string text = "The order must be filed by 4:00 PM.";

        var result = _sanitizer.Sanitize(text);

        Assert.Equal(text, result);
    }

    [Theory]
    [InlineData("Court’s order", "Court's order")]
    [InlineData("The “quoted” term", "The \"quoted\" term")]
    [InlineData("non\u00A0breaking\u00A0spaces", "non breaking spaces")]
    [InlineData("due Monday – Friday", "due Monday - Friday")]
    [InlineData("file now — no delay", "file now - no delay")]
    [InlineData("balance − payment", "balance - payment")]
    [InlineData("continue…", "continue...")]
    [InlineData("• first term", "* first term")]
    [InlineData("café résumé naïve façade", "cafe resume naive facade")]
    public void Sanitize_TransliteratesWordAndUnicodeCharacters(string input, string expected)
    {
        var result = _sanitizer.Sanitize(input);

        Assert.Equal(expected, result);
        Assert.All(result, c => Assert.True(c <= 127, $"Character '{c}' is not ASCII."));
    }
}
