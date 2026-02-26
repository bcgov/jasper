using System;
using System.Collections.Generic;
using JCCommon.Clients.FileServices;
using Mapster;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models;
using Scv.Api.Models.Civil.Detail;
using Scv.Api.Models.Criminal.Detail;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Infrastructure.Mappings;

public class BinderMappingTests
{
    private readonly TypeAdapterConfig _config;

    public BinderMappingTests()
    {
        _config = new TypeAdapterConfig();
        new BinderMapping().Register(_config);
    }

    #region BinderDto to Binder Mapping Tests

    [Fact]
    public void BinderDto_To_Binder_Should_Map_Properties_Correctly()
    {
        var binderDto = new BinderDto
        {
            Id = "binder-123",
            Labels = new Dictionary<string, string> { { "key1", "value1" } },
            Documents =
            [
                new() { DocumentId = "doc-1", Order = 0 }
            ]
        };

        var result = binderDto.Adapt<Binder>(_config);

        Assert.NotEqual(binderDto.Id, result.Id);
        Assert.Equal(binderDto.Labels, result.Labels);
        Assert.NotNull(result.Documents);
    }

    [Fact]
    public void BinderDto_To_Binder_Should_Ignore_Id()
    {
        var binderDto = new BinderDto
        {
            Id = "binder-123"
        };

        var binder = new Binder
        {
            Id = "existing-id"
        };

        binderDto.Adapt(binder, _config);

        Assert.Equal("existing-id", binder.Id);
        Assert.Equal("binder-123", binderDto.Id);
    }

    [Fact]
    public void BinderDto_To_Binder_Should_Ignore_Upd_Dtm()
    {
        var originalDate = DateTime.UtcNow.AddDays(-5);
        var binderDto = new BinderDto
        {
            Id = "binder-123",
            UpdatedDate = DateTime.UtcNow
        };

        var binder = new Binder
        {
            Upd_Dtm = originalDate
        };

        binderDto.Adapt(binder, _config);

        Assert.Equal(originalDate, binder.Upd_Dtm); // Upd_Dtm should remain unchanged
    }

    #endregion

    #region Binder to BinderDto Mapping Tests

    [Fact]
    public void Binder_To_BinderDto_Should_Map_Properties_Correctly()
    {
        var updatedDate = DateTime.UtcNow;
        var binder = new Binder
        {
            Id = "binder-123",
            Labels = new Dictionary<string, string> { { "key1", "value1" } },
            Upd_Dtm = updatedDate,
            Documents =
            [
                new() { DocumentId = "doc-1", Order = 0 }
            ]
        };

        var result = binder.Adapt<BinderDto>(_config);

        Assert.Equal(binder.Id, result.Id);
        Assert.Equal(binder.Labels, result.Labels);
        Assert.Equal(updatedDate, result.UpdatedDate);
        Assert.NotNull(result.Documents);
    }

    #endregion

    #region BinderDocumentDto to BinderDocument Mapping Tests

    [Fact]
    public void BinderDocumentDto_To_BinderDocument_Should_Map_DocumentType_As_Int()
    {
        var documentDto = new BinderDocumentDto
        {
            DocumentId = "doc-1",
            DocumentType = DocumentType.File,
            Order = 0
        };

        var result = documentDto.Adapt<BinderDocument>(_config);

        Assert.Equal(documentDto.DocumentId, result.DocumentId);
        Assert.Equal((int)DocumentType.File, result.DocumentType);
        Assert.Equal(documentDto.Order, result.Order);
    }

    [Theory]
    [InlineData(DocumentType.File, 0)]
    [InlineData(DocumentType.ROP, 1)]
    [InlineData(DocumentType.Report, 2)]
    [InlineData(DocumentType.CourtSummary, 3)]
    [InlineData(DocumentType.Transcript, 4)]
    public void BinderDocumentDto_To_BinderDocument_Should_Map_All_DocumentTypes(DocumentType docType, int expectedInt)
    {
        var documentDto = new BinderDocumentDto
        {
            DocumentId = "doc-1",
            DocumentType = docType,
            Order = 0
        };

        var result = documentDto.Adapt<BinderDocument>(_config);

        Assert.Equal(expectedInt, result.DocumentType);
    }

    #endregion

    #region BinderDocument to BinderDocumentDto Mapping Tests

    [Fact]
    public void BinderDocument_To_BinderDocumentDto_Should_Map_DocumentType_As_Enum()
    {
        var document = new BinderDocument
        {
            DocumentId = "doc-1",
            DocumentType = (int)DocumentType.CourtSummary,
            Order = 0
        };

        var result = document.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(document.DocumentId, result.DocumentId);
        Assert.Equal(DocumentType.CourtSummary, result.DocumentType);
        Assert.Equal(document.Order, result.Order);
    }

    [Theory]
    [InlineData(0, DocumentType.File)]
    [InlineData(1, DocumentType.ROP)]
    [InlineData(2, DocumentType.Report)]
    [InlineData(3, DocumentType.CourtSummary)]
    [InlineData(4, DocumentType.Transcript)]
    public void BinderDocument_To_BinderDocumentDto_Should_Map_All_DocumentTypes(int intValue, DocumentType expectedType)
    {
        var document = new BinderDocument
        {
            DocumentId = "doc-1",
            DocumentType = intValue,
            Order = 0
        };

        var result = document.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(expectedType, result.DocumentType);
    }

    #endregion

    #region CriminalDocument to BinderDocumentDto Mapping Tests

    [Fact]
    public void CriminalDocument_To_BinderDocumentDto_Should_Map_Properties_Correctly()
    {
        var criminalDoc = new CriminalDocument
        {
            DocmId = "crim-doc-123",
            ImageId = "image-456",
            DocumentTypeDescription = "Court Order",
            Category = "GENERAL"
        };

        var result = criminalDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(criminalDoc.ImageId, result.DocumentId);
        Assert.Equal(criminalDoc.DocumentTypeDescription, result.FileName);
        Assert.Equal(criminalDoc.Category, result.Category);
    }

    [Fact]
    public void CriminalDocument_To_BinderDocumentDto_Should_Map_ROP_Category_To_ROP_DocumentType()
    {
        var criminalDoc = new CriminalDocument
        {
            DocmId = "crim-doc-123",
            ImageId = "image-456",
            DocumentTypeDescription = "ROP Document",
            Category = DocumentCategory.ROP
        };

        var result = criminalDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(DocumentType.ROP, result.DocumentType);
    }

    [Theory]
    [InlineData("ROP")]
    [InlineData("rop")]
    [InlineData("RoP")]
    [InlineData("rOp")]
    public void CriminalDocument_To_BinderDocumentDto_Should_Map_ROP_Category_Case_Insensitive(string category)
    {
        var criminalDoc = new CriminalDocument
        {
            DocmId = "crim-doc-123",
            Category = category
        };

        var result = criminalDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(DocumentType.ROP, result.DocumentType);
    }

    [Theory]
    [InlineData("GENERAL")]
    [InlineData("OTHER")]
    [InlineData("")]
    [InlineData(null)]
    public void CriminalDocument_To_BinderDocumentDto_Should_Map_Non_ROP_Category_To_File_DocumentType(string category)
    {
        var criminalDoc = new CriminalDocument
        {
            DocmId = "crim-doc-123",
            Category = category
        };

        var result = criminalDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(DocumentType.File, result.DocumentType);
    }

    #endregion

    #region CivilDocument to BinderDocumentDto Mapping Tests

    [Fact]
    public void CivilDocument_To_BinderDocumentDto_Should_Map_Properties_Correctly()
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            ImageId = "image-789",
            DocumentTypeDescription = "Affidavit",
            DocumentTypeCd = "LITIGANT",
            Issue =
            [
                new() { IssueNumber = "1", IssueDsc = "Issue 1" }
            ]
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(civilDoc.CivilDocumentId, result.DocumentId);
        Assert.Equal(civilDoc.ImageId, result.ImageId);
        Assert.Equal(civilDoc.DocumentTypeDescription, result.FileName);
        Assert.Equal(civilDoc.DocumentTypeCd, result.Category);
        Assert.NotNull(result.Issues);
        Assert.Single(result.Issues);
    }

    [Fact]
    public void CivilDocument_To_BinderDocumentDto_Should_Map_CSR_Category_To_CourtSummary_DocumentType()
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            DocumentTypeCd = DocumentCategory.CSR,
            DocumentTypeDescription = "Court Summary Report"
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(DocumentType.CourtSummary, result.DocumentType);
    }

    [Theory]
    [InlineData("CSR")]
    [InlineData("csr")]
    [InlineData("CsR")]
    [InlineData("cSr")]
    public void CivilDocument_To_BinderDocumentDto_Should_Map_CSR_Category_Case_Insensitive(string category)
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            DocumentTypeCd = category
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(DocumentType.CourtSummary, result.DocumentType);
    }

    [Theory]
    [InlineData("LITIGANT")]
    [InlineData("OTHER")]
    [InlineData("")]
    [InlineData(null)]
    public void CivilDocument_To_BinderDocumentDto_Should_Map_Non_CSR_Category_To_File_DocumentType(string category)
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            DocumentTypeCd = category
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.Equal(DocumentType.File, result.DocumentType);
    }

    [Fact]
    public void CivilDocument_To_BinderDocumentDto_Should_Map_Empty_Issue_List()
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            Issue = []
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.NotNull(result.Issues);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void CivilDocument_To_BinderDocumentDto_Should_Handle_Null_Issue_List()
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            Issue = null
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.NotNull(result.Issues);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public void CivilDocument_To_BinderDocumentDto_Should_Handle_Null_FiledBy_List()
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            FiledBy = null
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.NotNull(result.FiledBy);
        Assert.Empty(result.FiledBy);
    }

    [Fact]
    public void CivilDocument_To_BinderDocumentDto_Should_Handle_Null_Issue_And_FiledBy()
    {
        var civilDoc = new CivilDocument
        {
            CivilDocumentId = "civil-doc-123",
            Issue = null,
            FiledBy = null
        };

        var result = civilDoc.Adapt<BinderDocumentDto>(_config);

        Assert.NotNull(result.Issues);
        Assert.Empty(result.Issues);
        Assert.NotNull(result.FiledBy);
        Assert.Empty(result.FiledBy);
    }

    #endregion

    #region CvfcIssue to IssueDto Mapping Tests

    [Fact]
    public void CvfcIssue_To_IssueDto_Should_Map_Properties()
    {
        var issue = new CvfcIssue
        {
            IssueNumber = "1",
            IssueDsc = "Test Issue"
        };

        var result = issue.Adapt<IssueDto>(_config);

        Assert.Equal(issue.IssueNumber, result.IssueNumber);
        Assert.Equal(issue.IssueDsc, result.IssueDsc);
    }

    #endregion

    #region CivilIssue to IssueDto Mapping Tests

    [Fact]
    public void CivilIssue_To_IssueDto_Should_Map_Properties()
    {
        var issue = new CivilIssue
        {
            IssueNumber = "2",
            IssueDsc = "Civil Issue"
        };

        var result = issue.Adapt<IssueDto>(_config);

        Assert.Equal(issue.IssueNumber, result.IssueNumber);
        Assert.Equal(issue.IssueDsc, result.IssueDsc);
    }

    #endregion

    #region ClFiledBy to FiledByDto Mapping Tests

    [Fact]
    public void ClFiledBy_To_FiledByDto_Should_Map_Properties()
    {
        var filedBy = new ClFiledBy
        {
            FiledByName = "John Doe"
        };

        var result = filedBy.Adapt<FiledByDto>(_config);

        Assert.Equal(filedBy.FiledByName, result.FiledByName);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Should_Map_Complete_Binder_With_All_Document_Types()
    {
        var binder = new Binder
        {
            Id = "binder-123",
            Upd_Dtm = DateTime.UtcNow,
            Documents =
            [
                new() { DocumentId = "doc-1", DocumentType = (int)DocumentType.File, Order = 0 },
                new() { DocumentId = "doc-2", DocumentType = (int)DocumentType.CourtSummary, Order = 1 },
                new() { DocumentId = "doc-3", DocumentType = (int)DocumentType.ROP, Order = 2 },
                new() { DocumentId = "doc-4", DocumentType = (int)DocumentType.Transcript, Order = 3 }
            ]
        };

        var result = binder.Adapt<BinderDto>(_config);

        Assert.Equal(4, result.Documents.Count);
        Assert.Equal(DocumentType.File, result.Documents[0].DocumentType);
        Assert.Equal(DocumentType.CourtSummary, result.Documents[1].DocumentType);
        Assert.Equal(DocumentType.ROP, result.Documents[2].DocumentType);
        Assert.Equal(DocumentType.Transcript, result.Documents[3].DocumentType);
    }

    [Fact]
    public void Should_Map_List_Of_CivilDocuments_To_BinderDocumentDtos()
    {
        var civilDocs = new List<CivilDocument>
        {
            new()
            {
                CivilDocumentId = "doc-1",
                DocumentTypeCd = DocumentCategory.CSR,
                ImageId = "image-1",
                DocumentTypeDescription = "Court Summary Report",
                FileSeqNo = "1",
                SwornByNm = "Jane Smith",
                Issue = [
                    new CivilIssue { IssueNumber = "1", IssueDsc = "Issue 1" }
                ],
                FiledBy = [
                    new ClFiledBy { FiledByName = "Attorney A" }
                ],
            },
            new()
            {
                CivilDocumentId = "doc-2",
                DocumentTypeCd = "LITIGANT",
                ImageId = "image-2",
                DocumentTypeDescription = "Affidavit",
                FileSeqNo = "2",
                SwornByNm = "John Doe",
                Issue = [],
                FiledBy = [
                    new ClFiledBy { FiledByName = "Attorney B" },
                    new ClFiledBy { FiledByName = "Attorney C" }
                ],
            }
        };

        var result = civilDocs.Adapt<List<BinderDocumentDto>>(_config);

        Assert.Equal(2, result.Count);

        // First document assertions
        Assert.Equal("doc-1", result[0].DocumentId);
        Assert.Equal(DocumentCategory.CSR, result[0].Category);
        Assert.Equal("image-1", result[0].ImageId);
        Assert.Equal("Court Summary Report", result[0].FileName);
        Assert.Equal(DocumentType.CourtSummary, result[0].DocumentType);
        Assert.Equal("1", result[0].FileSeqNo);
        Assert.Equal("Jane Smith", result[0].SwornByNm);
        Assert.Single(result[0].Issues);
        Assert.Equal("1", result[0].Issues[0].IssueNumber);
        Assert.Single(result[0].FiledBy);
        Assert.Equal("Attorney A", result[0].FiledBy[0].FiledByName);

        // Second document assertions
        Assert.Equal("doc-2", result[1].DocumentId);
        Assert.Equal("LITIGANT", result[1].Category);
        Assert.Equal("image-2", result[1].ImageId);
        Assert.Equal("Affidavit", result[1].FileName);
        Assert.Equal(DocumentType.File, result[1].DocumentType);
        Assert.Equal("2", result[1].FileSeqNo);
        Assert.Equal("John Doe", result[1].SwornByNm);
        Assert.Empty(result[1].Issues);
        Assert.Equal(2, result[1].FiledBy.Count);
        Assert.Equal("Attorney B", result[1].FiledBy[0].FiledByName);
        Assert.Equal("Attorney C", result[1].FiledBy[1].FiledByName);
    }

    [Fact]
    public void Should_Map_List_Of_CriminalDocuments_To_BinderDocumentDtos()
    {
        var criminalDocs = new List<CriminalDocument>
        {
            new() { DocmId = "doc-1", Category = DocumentCategory.ROP },
            new() { DocmId = "doc-2", Category = "OTHER" }
        };

        var result = criminalDocs.Adapt<List<BinderDocumentDto>>(_config);

        Assert.Equal(2, result.Count);
        Assert.Equal(DocumentType.ROP, result[0].DocumentType);
        Assert.Equal(DocumentType.File, result[1].DocumentType);
    }

    #endregion
}