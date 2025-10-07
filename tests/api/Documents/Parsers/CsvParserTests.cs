using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace tests.api.Documents.Parsers;

public class CsvParserTest
{
    private class TestRecord
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    [Theory]
    [InlineData("Name\tAge\nAlice\t30\nBob\t25", "\t")]
    [InlineData("Name,Age\nAlice,30\nBob,25", ",")]
    public void Parse_ReturnsCorrectRecords_UsingDifferentDelimiters(string csvContent, string delimiter)
    {
        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        var parser = new Scv.Api.Documents.Parsers.CsvParser();

        var result = parser.Parse<TestRecord>(csvStream, delimiter).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].Name);
        Assert.Equal(30, result[0].Age);
        Assert.Equal("Bob", result[1].Name);
        Assert.Equal(25, result[1].Age);
    }

    [Fact]
    public void Parse_ReturnsEmptyList_ForEmptyCsv()
    {
        var csvContent = "Name\tAge\n";
        var csvStream = new MemoryStream(Encoding.UTF8.GetBytes(csvContent));
        var parser = new Scv.Api.Documents.Parsers.CsvParser();

        var result = parser.Parse<TestRecord>(csvStream).ToList();

        Assert.Empty(result);
    }
}