using System.Collections.Generic;
using System.Linq;
using Scv.Db.Models;
using Scv.Models.Criminal.Detail;
using Scv.Models.Document;
using Scv.Models.Helpers.Extensions;
using Xunit;

namespace tests.api.Helpers.Documents;

public class KeyDocumentResolverTest
{
    [Fact]
    public void GetCriminalKeyDocuments_ReturnsEmpty_WhenDocumentsAreNull()
    {
        IEnumerable<CriminalDocument> documents = null;

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.Empty(result);
    }

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
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.INITIATING, IssueDate = "2024-01-02" },
            new() { Category = "OTHER", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.Category == DocumentCategories.ROP);
        Assert.Contains(resultList, d => d.Category == "Initiating");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsUncancelledBailDocument()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Active", IssueDate = "2024-01-05" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "CANCELLED", IssueDate = "2024-01-04" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("Active", resultList[0].DocmDispositionDsc);
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsKeyDocumentsAndUncancelledBail()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Active", IssueDate = "2024-01-02" },
            new() { Category = "OTHER", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.ROP));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.BAIL) && d.DocmDispositionDsc == "Active");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsPsrWithOtherKeyDocumentsAndBail()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.REPORT, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategories.INITIATING, IssueDate = "2024-01-03" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Active", IssueDate = "2024-01-04" },
            new() { Category = "OTHER", IssueDate = "2024-01-05" } // Should not be included
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(4, resultList.Count);

        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.REPORT));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.ROP));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.INITIATING));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.BAIL) && d.DocmDispositionDsc == "Active");
        Assert.DoesNotContain(resultList, d => d.Category == "OTHER");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ExcludesPsrFromOtherKeyDocuments_EvenWhenMultiplePsrsExist()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.PSR, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.PSR, IssueDate = "2024-01-05" },
            new() { Category = DocumentCategories.PSR, IssueDate = "2024-01-03" },
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategories.INITIATING, IssueDate = "2024-01-04" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(5, resultList.Count);

        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.REPORT));
        Assert.Contains(resultList, d => d.Category == DocumentCategories.ROP);
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.INITIATING));
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsOtherKeyDocuments_WhenNoPsrExists()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.INITIATING, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Active", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(3, resultList.Count);

        Assert.DoesNotContain(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.REPORT) || d.DocmClassification == DocumentCategory.Format(DocumentCategories.REPORT));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.ROP));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.INITIATING));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.BAIL) && d.DocmDispositionDsc == "Active");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ExcludesCancelledBailDocuments()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "CANCELLED", IssueDate = "2024-01-02" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "cancelled", IssueDate = "2024-01-03" }, // Case insensitive
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Active", IssueDate = "2024-01-01" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);

        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.ROP));
        Assert.Contains(resultList, d => d.Category == DocumentCategory.Format(DocumentCategories.BAIL) && d.DocmDispositionDsc == "Active");
        Assert.DoesNotContain(resultList, d => d.DocmDispositionDsc?.ToUpper() == "CANCELLED");
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsNoBailDocument_WhenAllBailDocumentsAreCancelled()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "CANCELLED", IssueDate = "2024-01-02" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Cancelled", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);

        Assert.Contains(resultList, d => d.Category == DocumentCategories.ROP);
        Assert.DoesNotContain(resultList, d => d.Category == DocumentCategories.BAIL);
    }

    [Fact]
    public void GetCriminalKeyDocuments_ReturnsLatestUncancelledBailDocument()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Active", IssueDate = "2024-01-02" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "CANCELLED", IssueDate = "2024-01-03" }, // More recent but cancelled
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "Pending", IssueDate = "2024-01-04" } // Most recent uncancelled
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);

        Assert.Contains(resultList, d => d.Category == DocumentCategories.ROP);
        var bailDocument = resultList.FirstOrDefault(d => d.Category == "Bail");
        Assert.NotNull(bailDocument);
        Assert.Equal("Pending", bailDocument.DocmDispositionDsc);
        Assert.Equal("2024-01-04", bailDocument.IssueDate);
    }

    [Fact]
    public void GetCriminalKeyDocuments_HandlesNullDocmDispositionDsc()
    {
        var documents = new List<CriminalDocument>
        {
            new() { Category = DocumentCategories.ROP, IssueDate = "2024-01-01" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = null, IssueDate = "2024-01-02" },
            new() { Category = DocumentCategories.BAIL, DocmDispositionDsc = "CANCELLED", IssueDate = "2024-01-03" }
        };

        var result = KeyDocumentResolver.GetCriminalKeyDocuments(documents);

        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);

        Assert.Contains(resultList, d => d.Category == DocumentCategories.ROP);
        var bailDocument = resultList.FirstOrDefault(d => d.Category == "Bail");
        Assert.NotNull(bailDocument);
        Assert.Null(bailDocument.DocmDispositionDsc);
        Assert.Equal("2024-01-02", bailDocument.IssueDate);
    }
}
