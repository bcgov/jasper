using Bogus;
using Scv.Core.Helpers.Extensions;
using Xunit;

namespace tests.api.Helpers.Extensions;

public class StringExtensionsTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void SplitFullNameToFirstAndLast_ReturnsExpected()
    {
        var last = _faker.Name.LastName();
        var first = _faker.Name.FirstName();

        var input = $"{last}, {first}";

        var (actualLast, actualFirst) = input.SplitFullNameToFirstAndLast();

        Assert.Equal(last, actualLast);
        Assert.Equal(first, actualFirst);
    }

    [Fact]
    public void SplitFullNameToFirstAndLast_WithMultipleFirstParts_ReturnsCombined()
    {
        var last = _faker.Name.LastName();
        var first = _faker.Name.FirstName();
        var middle = _faker.Name.FirstName();
        var suffix = "Jr.";

        var input = $"{last}, {first}, {middle}, {suffix}";

        var (actualLast, actualFirst) = input.SplitFullNameToFirstAndLast();

        Assert.Equal(last, actualLast);
        Assert.Equal($"{first},{middle},{suffix}", actualFirst);
    }

    [Fact]
    public void SplitFullNameToFirstAndLast_WithTrailingComma_NoFirstName()
    {
        var last = _faker.Name.LastName();
        var input = $"{last},";

        var (actualLast, actualFirst) = input.SplitFullNameToFirstAndLast();

        Assert.Equal(last, actualLast);
        Assert.Equal(string.Empty, actualFirst);
    }

    [Fact]
    public void SplitFullNameToFirstAndLast_WithLastNameOnly_ReturnsLast()
    {
        var last = _faker.Name.LastName();

        var (actualLast, actualFirst) = last.SplitFullNameToFirstAndLast();

        Assert.Equal(last, actualLast);
        Assert.Equal(string.Empty, actualFirst);
    }

    [Fact]
    public void SplitFullNameToFirstAndLast_CustomDelimiter_Pipe()
    {
        var last = _faker.Name.LastName();
        var first = _faker.Name.FirstName();
        var middle = _faker.Name.FirstName();

        var input = $"{last}|{first}|{middle}";

        var (actualLast, actualFirst) = input.SplitFullNameToFirstAndLast("|");

        Assert.Equal(last, actualLast);
        Assert.Equal($"{first}|{middle}", actualFirst);
    }

    [Fact]
    public void SplitFullNameToFirstAndLast_MultipleRandomGenerated_Stability()
    {
        // Run several randomized cases to ensure invariant behavior
        for (int i = 0; i < 5; i++)
        {
            var last = _faker.Name.LastName();
            var first = _faker.Name.FirstName();

            var input = $"{last}, {first}";

            var (actualLast, actualFirst) = input.SplitFullNameToFirstAndLast();

            Assert.Equal(last, actualLast);
            Assert.Equal(first, actualFirst);
        }
    }

    [Fact]
    public void SplitFullNameToFirstAndLast_NullInput_ReturnsNullAndEmpty()
    {
        string fullName = null;

        var (last, first) = fullName.SplitFullNameToFirstAndLast();

        Assert.Equal(string.Empty, last);
        Assert.Equal(string.Empty, first);
    }

    [Fact]
    public void SplitFullNameToFirstAndLast_WhitespaceInput_ReturnsOriginalAndEmpty()
    {
        var input = "   ";

        var (last, first) = input.SplitFullNameToFirstAndLast();

        Assert.Equal(input, last);
        Assert.Equal(string.Empty, first);
    }

    [Fact]
    public void GetUtf8Size_NullInput_ReturnsZero()
    {
        string input = null;

        var size = input.GetUtf8Size();

        Assert.Equal(0, size);
    }

    [Fact]
    public void GetUtf8Size_AsciiInput_ReturnsCharacterCount()
    {
        var input = "abc";

        var size = input.GetUtf8Size();

        Assert.Equal(3, size);
    }

    [Fact]
    public void GetUtf8Size_MultiByteInput_ReturnsUtf8ByteCount()
    {
        var input = "caf\u00e9";

        var size = input.GetUtf8Size();

        Assert.Equal(5, size);
    }

    [Fact]
    public void CompressAndDecompressFromBase64_RoundTripsOriginal()
    {
        var input = "hello world";

        var compressed = input.CompressToBase64();
        var decompressed = compressed.DecompressFromBase64();

        Assert.False(string.IsNullOrWhiteSpace(compressed));
        Assert.Equal(input, decompressed);
    }

    [Fact]
    public void CompressToBase64_WhitespaceInput_ReturnsEmpty()
    {
        var input = "   ";

        var compressed = input.CompressToBase64();

        Assert.Equal(string.Empty, compressed);
    }

    [Fact]
    public void DecompressFromBase64_WhitespaceInput_ReturnsEmpty()
    {
        var decompressed = "   ".DecompressFromBase64();

        Assert.Equal(string.Empty, decompressed);
    }
}
