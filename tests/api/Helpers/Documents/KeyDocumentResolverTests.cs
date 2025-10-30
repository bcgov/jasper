using System.Collections.Generic;
using System.Linq;
using Scv.Api.Helpers.Documents;
using Scv.Api.Models.Criminal.Detail;
using Scv.Db.Models;
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
            new() { Category = DocumentCategory.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategory.INITIATING, IssueDate = "2024-01-02" },
            new() { Category = "OTHER", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.ROP);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.INITIATING);
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsPerfectedBailDocument()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategory.BAIL, DocmDispositionDsc = "Perfected", IssueDate = "2024-01-05" },
            new() { Category = DocumentCategory.BAIL, DocmDispositionDsc = "Not Perfected", IssueDate = "2024-01-04" }
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
            new() { Category = DocumentCategory.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategory.BAIL, DocmDispositionDsc = "Perfected", IssueDate = "2024-01-02" },
            new() { Category = "OTHER", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.ROP);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.BAIL && d.DocmDispositionDsc == "Perfected");
    }

    [Fact]
    public void GetCriminalKeyDocuments_HandlesNullCategoryAndClassification()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = null, DocmClassification = DocumentCategory.ROP, IssueDate = "2024-01-01" },
            new() { Category = null, DocmClassification = DocumentCategory.BAIL, DocmDispositionDsc = "Perfected", IssueDate = "2024-01-02" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.DocmClassification == DocumentCategory.ROP);
        Assert.Contains(resultList, d => d.DocmClassification == DocumentCategory.BAIL && d.DocmDispositionDsc == "Perfected");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsLatestPsrDocument_WhenMultiplePsrsExist()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategory.PSR, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategory.PSR, IssueDate = "2024-01-03" }, // Most recent
            new() { Category = DocumentCategory.PSR, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategory.ROP, IssueDate = "2024-01-04" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        
        var psrDocument = resultList.FirstOrDefault(d => d.Category == DocumentCategory.PSR);
        Assert.NotNull(psrDocument);
        Assert.Equal("2024-01-03", psrDocument.IssueDate);
        
        Assert.Contains(resultList, d => d.Category == DocumentCategory.ROP);
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsPsrWithOtherKeyDocumentsAndBail()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategory.PSR, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategory.ROP, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategory.INITIATING, IssueDate = "2024-01-03" },
            new() { Category = DocumentCategory.BAIL, DocmDispositionDsc = "Perfected", IssueDate = "2024-01-04" },
            new() { Category = "OTHER", IssueDate = "2024-01-05" } // Should not be included
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(4, resultList.Count);
        
        Assert.Contains(resultList, d => d.Category == DocumentCategory.PSR);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.ROP);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.INITIATING);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.BAIL && d.DocmDispositionDsc == "Perfected");
        Assert.DoesNotContain(resultList, d => d.Category == "OTHER");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ExcludesPsrFromOtherKeyDocuments_EvenWhenMultiplePsrsExist()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategory.PSR, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategory.PSR, IssueDate = "2024-01-05" }, // Most recent PSR
            new() { Category = DocumentCategory.PSR, IssueDate = "2024-01-03" },
            new() { Category = DocumentCategory.ROP, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategory.INITIATING, IssueDate = "2024-01-04" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count); // Only 1 PSR (most recent) + ROP + INITIATING
        
        // Should have only the most recent PSR
        var psrDocuments = resultList.Where(d => d.Category == DocumentCategory.PSR).ToList();
        Assert.Single(psrDocuments);
        Assert.Equal("2024-01-05", psrDocuments[0].IssueDate);
        
        Assert.Contains(resultList, d => d.Category == DocumentCategory.ROP);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.INITIATING);
    }

    [Fact]
    public void GetCriminalKeyDocuments_FindsPsrUsingDocmClassification_WhenCategoryIsNull()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = null, DocmClassification = DocumentCategory.PSR, IssueDate = "2024-01-01" },
            new() { Category = null, DocmClassification = DocumentCategory.PSR, IssueDate = "2024-01-03" }, // Most recent
            new() { Category = DocumentCategory.ROP, IssueDate = "2024-01-02" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        
        var psrDocument = resultList.FirstOrDefault(d => d.DocmClassification == DocumentCategory.PSR);
        Assert.NotNull(psrDocument);
        Assert.Equal("2024-01-03", psrDocument.IssueDate); // Should be the most recent
        Assert.Null(psrDocument.Category);
        
        Assert.Contains(resultList, d => d.Category == DocumentCategory.ROP);
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsOtherKeyDocuments_WhenNoPsrExists()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategory.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategory.INITIATING, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategory.BAIL, DocmDispositionDsc = "Perfected", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);
        
        Assert.DoesNotContain(resultList, d => d.Category == DocumentCategory.PSR || d.DocmClassification == DocumentCategory.PSR);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.ROP);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.INITIATING);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.BAIL && d.DocmDispositionDsc == "Perfected");
    }
}