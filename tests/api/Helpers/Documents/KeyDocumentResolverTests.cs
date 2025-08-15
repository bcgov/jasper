using System.Collections.Generic;
using System.Linq;
using Scv.Api.Helpers.Documents;
using Scv.Api.Models.Criminal.Detail;
using Xunit;

namespace tests.api.Helpers.Documents;

public class KeyDocumentResolverTest
{
    [Fact]
    public void GetCriminalKeyDocuments_ReturnsEmpty_WhenNoDocuments()
    {
        var documents = Enumerable.Empty<CriminalDocument>();

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.Empty(result);
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsKeyDocuments_ByCategory()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = "ROP", IssueDate = "2024-01-01" },
            new() { Category = "INITIATING", IssueDate = "2024-01-02" },
            new() { Category = "OTHER", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.Category == "ROP");
        Assert.Contains(resultList, d => d.Category == "INITIATING");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsPerfectedBailDocument()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = "BAIL", DocmDispositionDsc = "Perfected", IssueDate = "2024-01-05" },
            new() { Category = "BAIL", DocmDispositionDsc = "Not Perfected", IssueDate = "2024-01-04" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("Perfected", resultList[0].DocmDispositionDsc);
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsKeyDocumentsAndPerfectedBail()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = "ROP", IssueDate = "2024-01-01" },
            new() { Category = "BAIL", DocmDispositionDsc = "Perfected", IssueDate = "2024-01-02" },
            new() { Category = "OTHER", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.Category == "ROP");
        Assert.Contains(resultList, d => d.Category == "BAIL" && d.DocmDispositionDsc == "Perfected");
    }

    [Fact]
    public void GetCriminalKeyDocuments_HandlesNullCategoryAndClassification()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = null, DocmClassification = "ROP", IssueDate = "2024-01-01" },
            new() { Category = null, DocmClassification = "BAIL", DocmDispositionDsc = "Perfected", IssueDate = "2024-01-02" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.DocmClassification == "ROP");
        Assert.Contains(resultList, d => d.DocmClassification == "BAIL" && d.DocmDispositionDsc == "Perfected");
    }
}