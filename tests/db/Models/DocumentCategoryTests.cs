using Scv.Db.Models;
using Xunit;

namespace tests.db.Models;

public class DocumentCategoryTests
{
    [Fact]
    public void Format_ReturnReport_WhenPsrProvided()
    {
        var result = DocumentCategory.Format("PSR");

        Assert.Equal("Report", result);
    }

    [Fact]
    public void Format_ReturnReport_WhenPsrProvidedInLowercase()
    {
        var result = DocumentCategory.Format("psr");

        Assert.Equal("Report", result);
    }

    [Fact]
    public void Format_ReturnReport_WhenPsrProvidedInMixedCase()
    {
        var result = DocumentCategory.Format("PsR");

        Assert.Equal("Report", result);
    }

    [Fact]
    public void Format_ReturnROP_WhenRopProvided()
    {
        var result = DocumentCategory.Format("ROP");

        Assert.Equal("ROP", result);
    }

    [Fact]
    public void Format_ReturnROP_WhenRopProvidedInLowercase()
    {
        var result = DocumentCategory.Format("rop");

        Assert.Equal("ROP", result);
    }

    [Fact]
    public void Format_ReturnROP_WhenRopProvidedInMixedCase()
    {
        var result = DocumentCategory.Format("RoP");

        Assert.Equal("ROP", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenBailProvided()
    {
        var result = DocumentCategory.Format("BAIL");

        Assert.Equal("Bail", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenInitiatingProvided()
    {
        var result = DocumentCategory.Format("INITIATING");

        Assert.Equal("Initiating", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenAffidavitsProvided()
    {
        var result = DocumentCategory.Format("AFFIDAVITS");

        Assert.Equal("Affidavits", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenMotionsProvided()
    {
        var result = DocumentCategory.Format("MOTIONS");

        Assert.Equal("Motions", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenOrdersProvided()
    {
        var result = DocumentCategory.Format("ORDERS");

        Assert.Equal("Orders", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenPleadingsProvided()
    {
        var result = DocumentCategory.Format("PLEADINGS");

        Assert.Equal("Pleadings", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenLowerCaseProvided()
    {
        var result = DocumentCategory.Format("bail");

        Assert.Equal("Bail", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenMixedCaseProvided()
    {
        var result = DocumentCategory.Format("BaIl");

        Assert.Equal("Bail", result);
    }

    [Fact]
    public void Format_ReturnEmptyString_WhenNullProvided()
    {
        var result = DocumentCategory.Format(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Format_ReturnEmptyString_WhenEmptyStringProvided()
    {
        var result = DocumentCategory.Format(string.Empty);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Format_ReturnEmptyString_WhenWhitespaceProvided()
    {
        var result = DocumentCategory.Format("   ");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenSingleCharacterProvided()
    {
        var result = DocumentCategory.Format("A");

        Assert.Equal("A", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenTwoCharactersProvided()
    {
        var result = DocumentCategory.Format("AB");

        Assert.Equal("Ab", result);
    }

    [Fact]
    public void Format_ReturnTitleCase_WhenArbitraryStringProvided()
    {
        var result = DocumentCategory.Format("SOME_CATEGORY");

        Assert.Equal("Some_category", result);
    }
}
