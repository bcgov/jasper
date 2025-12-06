using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Helpers;
using Scv.Api.Models;
using Scv.Api.Processors;
using Scv.Db.Contants;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Processors;

public class JudicialBinderProcessorTests
{
    private readonly Mock<FileServicesClient> _mockFilesClient;
    private readonly Mock<IAppCache> _mockCache;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IValidator<BinderDto>> _mockValidator;
    private readonly ClaimsPrincipal _mockUser;

    public JudicialBinderProcessorTests()
    {
        var httpClient = new System.Net.Http.HttpClient();
        _mockFilesClient = new Mock<FileServicesClient>(httpClient);
        _mockCache = new Mock<IAppCache>();
        _mockConfiguration = new Mock<IConfiguration>();
        _mockMapper = new Mock<IMapper>();
        _mockValidator = new Mock<IValidator<BinderDto>>();

        var claims = new List<Claim>
        {
            new(CustomClaimTypes.UserId, "test-judge-123"),
            new(CustomClaimTypes.JcParticipantId, "test-participant"),
            new(CustomClaimTypes.JcAgencyCode, "test-agency")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _mockUser = new ClaimsPrincipal(identity);

        _mockValidator
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<BinderDto>>(), default))
            .ReturnsAsync(new FluentValidation.Results.ValidationResult());
    }

    [Fact]
    public async Task PreProcessAsync_Should_Add_CourtClassCd_And_JudgeId_Labels()
    {
        var fileDetail = new CivilFileDetailResponse
        {
            CourtClassCd = CivilFileDetailResponseCourtClassCd.F
        };

        _mockFilesClient
            .Setup(x => x.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fileDetail);

        _mockConfiguration
            .Setup(x => x.GetSection("Request:ApplicationCd").Value)
            .Returns("TEST_APP");

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            }
        };

        var processor = new JudicialBinderProcessor(
            _mockFilesClient.Object,
            _mockUser,
            _mockValidator.Object,
            dto,
            _mockCache.Object,
            _mockConfiguration.Object,
            _mockMapper.Object);

        await processor.PreProcessAsync();

        Assert.Equal("F", processor.Binder.Labels[LabelConstants.COURT_CLASS_CD]);
        Assert.Equal("test-judge-123", processor.Binder.Labels[LabelConstants.JUDGE_ID]);
    }

    [Fact]
    public async Task ProcessAsync_Should_Return_Failure_When_Binder_Id_Is_Null()
    {
        var dto = new BinderDto
        {
            Id = null,
            Labels = []
        };

        var processor = new JudicialBinderProcessor(
            _mockFilesClient.Object,
            _mockUser,
            _mockValidator.Object,
            dto,
            _mockCache.Object,
            _mockConfiguration.Object,
            _mockMapper.Object);

        var result = await processor.ProcessAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Binder does not exist.", result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_JudgeId_Does_Not_Match_CurrentUser()
    {
        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.JUDGE_ID, "different-judge-456" },
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            }
        };

        var processor = new JudicialBinderProcessor(
            _mockFilesClient.Object,
            _mockUser,
            _mockValidator.Object,
            dto,
            _mockCache.Object,
            _mockConfiguration.Object,
            _mockMapper.Object);

        var result = await processor.ValidateAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Current user does not have access to this binder.", result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_Document_IDs_Are_Invalid()
    {
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance =
            [
                new CvfcAppearance { AppearanceId = "appearance-1" }
            ],
            Document =
            [
                new CvfcDocument3 { CivilDocumentId = "doc-1" }
            ],
            ReferenceDocument =
            [
                new CvfcRefDocument3 { ReferenceDocumentId = "ref-1" }
            ]
        };

        _mockFilesClient
            .Setup(x => x.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fileDetail);

        _mockConfiguration
            .Setup(x => x.GetSection("Request:ApplicationCd").Value)
            .Returns("TEST_APP");

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.JUDGE_ID, "test-judge-123" },
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents =
            [
                new BinderDocumentDto { DocumentId = "invalid-doc-id" }
            ]
        };

        var processor = new JudicialBinderProcessor(
            _mockFilesClient.Object,
            _mockUser,
            _mockValidator.Object,
            dto,
            _mockCache.Object,
            _mockConfiguration.Object,
            _mockMapper.Object);

        var result = await processor.ValidateAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Found one or more invalid Document IDs.", result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Success_When_All_Document_IDs_Are_Valid()
    {
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance =
            [
                new CvfcAppearance { AppearanceId = "appearance-1" }
            ],
            Document =
            [
                new CvfcDocument3 { CivilDocumentId = "doc-1" }
            ],
            ReferenceDocument =
            [
                new CvfcRefDocument3 { ReferenceDocumentId = "ref-1" }
            ]
        };

        _mockFilesClient
            .Setup(x => x.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fileDetail);

        _mockConfiguration
            .Setup(x => x.GetSection("Request:ApplicationCd").Value)
            .Returns("TEST_APP");

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.JUDGE_ID, "test-judge-123" },
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents =
            [
                new BinderDocumentDto { DocumentId = "appearance-1" },
                new BinderDocumentDto { DocumentId = "doc-1" },
                new BinderDocumentDto { DocumentId = "ref-1" }
            ]
        };

        var processor = new JudicialBinderProcessor(
            _mockFilesClient.Object,
            _mockUser,
            _mockValidator.Object,
            dto,
            _mockCache.Object,
            _mockConfiguration.Object,
            _mockMapper.Object);

        var result = await processor.ValidateAsync();

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void PopulateDetailCsrsDocuments_Should_Create_CSR_Documents()
    {
        var appearances = new List<CvfcAppearance>
        {
            new() { AppearanceId = "app-1" },
            new() { AppearanceId = "app-2" }
        };

        var result = JudicialBinderProcessor_TestHelper.CallPopulateDetailCsrsDocuments(appearances);

        Assert.Equal(2, result.Count());
        Assert.All(result, doc =>
        {
            Assert.Equal(DocumentCategory.CSR, doc.DocumentTypeCd);
            Assert.Equal("Court Summary", doc.DocumentTypeDescription);
        });
    }

    [Fact]
    public void PopulateDetailReferenceDocuments_Should_Create_Reference_Documents()
    {
        var referenceDocuments = new List<CvfcRefDocument3>
        {
            new() {
                ObjectGuid = "guid-1",
                ReferenceDocumentTypeDsc = "Type 1"
            },
            new() {
                ObjectGuid = "guid-2",
                ReferenceDocumentTypeDsc = "Type 2"
            }
        };

        var result = JudicialBinderProcessor_TestHelper.CallPopulateDetailReferenceDocuments(referenceDocuments);

        Assert.Equal(2, result.Count());
        Assert.All(result, doc =>
        {
            Assert.Equal(DocumentCategory.LITIGANT, doc.DocumentTypeCd);
        });
    }
}

// Helper class to access private static methods for testing
public static class JudicialBinderProcessor_TestHelper
{
    public static IEnumerable<CvfcDocument> CallPopulateDetailCsrsDocuments(ICollection<CvfcAppearance> appearances)
    {
        return appearances.Select(appearance => new CvfcDocument()
        {
            DocumentTypeCd = DocumentCategory.CSR,
            DocumentTypeDescription = "Court Summary",
            DocumentId = appearance.AppearanceId,
            ImageId = appearance.AppearanceId,
        });
    }

    public static IEnumerable<CvfcDocument> CallPopulateDetailReferenceDocuments(ICollection<CvfcRefDocument3> referenceDocuments)
    {
        return referenceDocuments.Select(referenceDocument => new CvfcDocument()
        {
            DocumentTypeCd = DocumentCategory.LITIGANT,
            DocumentTypeDescription = referenceDocument.ReferenceDocumentTypeDsc,
            DocumentId = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(System.Text.Encoding.UTF8.GetBytes(referenceDocument.ObjectGuid)),
            ImageId = referenceDocument.ObjectGuid,
        });
    }
}
