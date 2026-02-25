using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Mocks;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Documents;
using Scv.Api.Helpers;
using Scv.Api.Models;
using Scv.Api.Models.Civil.Detail;
using Scv.Api.Processors;
using Scv.Api.Services;
using Scv.Db.Contants;
using Xunit;

namespace tests.api.Processors;

public class JudicialBinderProcessorTests
{
    private readonly Mock<FileServicesClient> _mockFilesClient;
    private readonly Mock<IDarsService> _mockDarsService;
    private readonly IAppCache _mockCache;
    private readonly Mock<IConfiguration> _mockConfiguration;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<IValidator<BinderDto>> _mockValidator;
    private readonly ClaimsPrincipal _mockUser;

    public JudicialBinderProcessorTests()
    {
        var httpClient = new System.Net.Http.HttpClient();
        _mockFilesClient = new Mock<FileServicesClient>(httpClient);
        _mockDarsService = new Mock<IDarsService>();
        _mockCache = new MockCachingService();
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

        _mockConfiguration
            .Setup(x => x.GetSection("Request:ApplicationCd").Value)
            .Returns("TEST_APP");
    }

    #region PreProcessAsync Tests

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

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            }
        };

        var processor = CreateProcessor(dto);
        await processor.PreProcessAsync();

        Assert.Equal("F", processor.Binder.Labels[LabelConstants.COURT_CLASS_CD]);
        Assert.Equal("test-judge-123", processor.Binder.Labels[LabelConstants.JUDGE_ID]);
        Assert.Equal("false", processor.Binder.Labels[LabelConstants.IS_CRIMINAL]);
    }

    #endregion

    #region ProcessAsync Tests

    [Fact]
    public async Task ProcessAsync_Should_Return_Success_And_Populate_Documents()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            CourtClassCd = CivilFileDetailResponseCourtClassCd.F,
            Appearance = [],
            Document = [
                new CvfcDocument3 { CivilDocumentId = "doc-1" }
            ],
            ReferenceDocument = []
        };

        var fileContent = new CivilFileContent
        {
            CivilFile = []
        };

        var redactedDetail = new RedactedCivilFileDetailResponse
        {
            Document = [
                new CivilDocument
                {
                    CivilDocumentId = "doc-1",
                    DocumentTypeDescription = "Test Document"
                }
            ]
        };

        var expectedDocuments = new List<BinderDocumentDto>
        {
            new() { DocumentId = "doc-1", Order = 0, FileName = "Test Document" }
        };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(redactedDetail);

        _mockMapper
            .Setup(x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()))
            .Returns(expectedDocuments);

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" },
                { LabelConstants.JUDGE_ID, "test-judge-123" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "doc-1", Order = 0 }
            ]
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(processor.Binder.Documents);
        Assert.Single(processor.Binder.Documents);
        Assert.Equal("doc-1", processor.Binder.Documents[0].DocumentId);
    }

    [Fact]
    public async Task ProcessAsync_Should_Return_Success_With_Empty_Documents_When_No_Matches()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            CourtClassCd = CivilFileDetailResponseCourtClassCd.F,
            Appearance = [],
            Document = [],
            ReferenceDocument = []
        };

        var fileContent = new CivilFileContent { CivilFile = [] };
        var redactedDetail = new RedactedCivilFileDetailResponse { Document = [] };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(redactedDetail);

        _mockMapper
            .Setup(x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()))
            .Returns([]);

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = []
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.NotNull(processor.Binder.Documents);
        Assert.Empty(processor.Binder.Documents);
    }

    [Fact]
    public async Task ProcessAsync_Should_Preserve_Document_Order()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [
                new CvfcAppearance { AppearanceId = "app-1" }
            ],
            Document = [
                new CvfcDocument3 { CivilDocumentId = "doc-1" }
            ],
            ReferenceDocument = [
                new CvfcRefDocument3 { ReferenceDocumentId = "ref-1", ReferenceDocumentInterest = [] }
            ]
        };

        var fileContent = new CivilFileContent { CivilFile = [] };
        var redactedDetail = new RedactedCivilFileDetailResponse
        {
            Document = [
                new CivilDocument { CivilDocumentId = "doc-1" }
            ]
        };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(redactedDetail);

        _mockMapper
            .Setup(x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()))
            .Returns([
                new BinderDocumentDto { DocumentId = "doc-1", Order = 2 },
                new BinderDocumentDto { DocumentId = "app-1", Order = 0 },
                new BinderDocumentDto { DocumentId = "ref-1", Order = 1 }
            ]);

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "doc-1", Order = 2 },
                new BinderDocumentDto { DocumentId = "app-1", Order = 0 },
                new BinderDocumentDto { DocumentId = "ref-1", Order = 1 }
            ]
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, processor.Binder.Documents.Count);

        // Verify documents are ordered correctly
        Assert.Equal("app-1", processor.Binder.Documents[0].DocumentId);
        Assert.Equal(0, processor.Binder.Documents[0].Order);

        Assert.Equal("ref-1", processor.Binder.Documents[1].DocumentId);
        Assert.Equal(1, processor.Binder.Documents[1].Order);

        Assert.Equal("doc-1", processor.Binder.Documents[2].DocumentId);
        Assert.Equal(2, processor.Binder.Documents[2].Order);

        // Verify mapper was called with the correct input
        _mockMapper.Verify(
            x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()),
            Times.Once);
    }

    [Fact]
    public async Task ProcessAsync_Should_Only_Include_Documents_In_BinderDto()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [
                new CvfcAppearance { AppearanceId = "app-1" },
                new CvfcAppearance { AppearanceId = "app-2" },
                new CvfcAppearance { AppearanceId = "app-3" }
            ],
            Document = [],
            ReferenceDocument = []
        };

        var fileContent = new CivilFileContent { CivilFile = [] };
        var redactedDetail = new RedactedCivilFileDetailResponse { Document = [] };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(redactedDetail);

        _mockMapper
            .Setup(x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()))
            .Returns([
                new BinderDocumentDto { DocumentId = "app-1", Order = 0 }
            ]);

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "app-1", Order = 0 }
                // app-2 and app-3 are NOT in the binder
            ]
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(processor.Binder.Documents);
        Assert.Equal("app-1", processor.Binder.Documents[0].DocumentId);
    }

    [Fact]
    public async Task ProcessAsync_Should_Include_CSR_Documents()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [
                new CvfcAppearance { AppearanceId = "app-1", AppearanceDate = "2024-01-01" }
            ],
            Document = [],
            ReferenceDocument = []
        };

        var fileContent = new CivilFileContent { CivilFile = [] };
        var redactedDetail = new RedactedCivilFileDetailResponse { Document = [] };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(redactedDetail);

        _mockMapper
            .Setup(x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()))
            .Returns([
                new BinderDocumentDto
                {
                    DocumentId = "app-1",
                    Order = 0,
                    DocumentType = DocumentType.CourtSummary
                }
            ]);

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "app-1", Order = 0 }
            ]
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(processor.Binder.Documents);
        Assert.Equal(DocumentType.CourtSummary, processor.Binder.Documents[0].DocumentType);
    }

    [Fact]
    public async Task ProcessAsync_Should_Include_Reference_Documents()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [],
            Document = [],
            ReferenceDocument = [
                new CvfcRefDocument3
                {
                    ReferenceDocumentId = "ref-1",
                    ObjectGuid = "guid-1",
                    ReferenceDocumentInterest = []
                }
            ]
        };

        var fileContent = new CivilFileContent { CivilFile = [] };
        var redactedDetail = new RedactedCivilFileDetailResponse { Document = [] };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(redactedDetail);

        _mockMapper
            .Setup(x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()))
            .Returns([
                new BinderDocumentDto { DocumentId = "ref-1", Order = 0 }
            ]);

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "ref-1", Order = 0 }
            ]
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Single(processor.Binder.Documents);
        Assert.Equal("ref-1", processor.Binder.Documents[0].DocumentId);
    }

    [Fact]
    public async Task ProcessAsync_Should_Return_Failure_When_FileClient_Throws_Exception()
    {
        // Arrange
        _mockFilesClient
            .Setup(x => x.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ThrowsAsync(new Exception("File service error"));

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = []
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Error processing binder: File service error", result.Errors);
    }

    [Fact]
    public async Task ProcessAsync_Should_Return_Failure_When_Mapper_Throws_Exception()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [],
            Document = [],
            ReferenceDocument = []
        };

        var fileContent = new CivilFileContent { CivilFile = [] };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Throws(new Exception("Mapping error"));

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = []
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.False(result.Succeeded);
        Assert.Contains("Error processing binder: Mapping error", result.Errors);
    }

    [Fact]
    public async Task ProcessAsync_Should_Handle_Mixed_Document_Types()
    {
        // Arrange
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [
                new CvfcAppearance { AppearanceId = "app-1" }
            ],
            Document = [
                new CvfcDocument3 { CivilDocumentId = "doc-1" }
            ],
            ReferenceDocument = [
                new CvfcRefDocument3
                {
                    ReferenceDocumentId = "ref-1",
                    ReferenceDocumentInterest = []
                }
            ]
        };

        var fileContent = new CivilFileContent { CivilFile = [] };
        var redactedDetail = new RedactedCivilFileDetailResponse
        {
            Document = [
                new CivilDocument { CivilDocumentId = "doc-1" }
            ]
        };

        SetupFileClientMocks(fileDetail, fileContent);

        _mockMapper
            .Setup(x => x.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(redactedDetail);

        _mockMapper
            .Setup(x => x.Map<List<BinderDocumentDto>>(It.IsAny<IEnumerable<CivilDocument>>()))
            .Returns([
                new BinderDocumentDto { DocumentId = "doc-1", Order = 1 },
                new BinderDocumentDto { DocumentId = "app-1", Order = 0 },
                new BinderDocumentDto { DocumentId = "ref-1", Order = 2 }
            ]);

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "doc-1", Order = 1 },
                new BinderDocumentDto { DocumentId = "app-1", Order = 0 },
                new BinderDocumentDto { DocumentId = "ref-1", Order = 2 }
            ]
        };

        var processor = CreateProcessor(dto);

        // Act
        var result = await processor.ProcessAsync();

        // Assert
        Assert.True(result.Succeeded);
        Assert.Equal(3, processor.Binder.Documents.Count);


    }

    #endregion

    #region ValidateAsync Tests

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

        var processor = CreateProcessor(dto);
        var result = await processor.ValidateAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Current user does not have access to this binder.", result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Error_When_Document_IDs_Are_Invalid()
    {
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [
                new CvfcAppearance { AppearanceId = "appearance-1" }
            ],
            Document = [
                new CvfcDocument3 { CivilDocumentId = "doc-1" }
            ],
            ReferenceDocument = [
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

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.JUDGE_ID, "test-judge-123" },
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "invalid-doc-id", DocumentType = DocumentType.File }
            ]
        };

        var processor = CreateProcessor(dto);
        var result = await processor.ValidateAsync();

        Assert.False(result.Succeeded);
        Assert.Contains("Found one or more invalid Document IDs.", result.Errors);
    }

    [Fact]
    public async Task ValidateAsync_Should_Return_Success_When_All_Document_IDs_Are_Valid()
    {
        var fileDetail = new CivilFileDetailResponse
        {
            Appearance = [
                new CvfcAppearance { AppearanceId = "appearance-1" }
            ],
            Document = [
                new CvfcDocument3 { CivilDocumentId = "doc-1" }
            ],
            ReferenceDocument = [
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

        var dto = new BinderDto
        {
            Labels = new Dictionary<string, string>
            {
                { LabelConstants.JUDGE_ID, "test-judge-123" },
                { LabelConstants.PHYSICAL_FILE_ID, "12345" }
            },
            Documents = [
                new BinderDocumentDto { DocumentId = "appearance-1", DocumentType = DocumentType.CourtSummary },
                new BinderDocumentDto { DocumentId = "doc-1", DocumentType = DocumentType.File },
                new BinderDocumentDto { DocumentId = "ref-1", DocumentType = DocumentType.File }
            ]
        };

        var processor = CreateProcessor(dto);
        var result = await processor.ValidateAsync();

        Assert.True(result.Succeeded);
        Assert.Empty(result.Errors);
    }

    #endregion

    #region Private Helper Methods

    private JudicialBinderProcessor CreateProcessor(BinderDto dto)
    {
        return new JudicialBinderProcessor(
            _mockFilesClient.Object,
            _mockUser,
            _mockValidator.Object,
            dto,
            _mockCache,
            _mockMapper.Object,
            _mockConfiguration.Object,
            _mockDarsService.Object,
            null);
    }

    private void SetupFileClientMocks(CivilFileDetailResponse fileDetail, CivilFileContent fileContent)
    {
        _mockFilesClient
            .Setup(x => x.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fileDetail);

        _mockFilesClient
            .Setup(x => x.FilesCivilFilecontentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync(fileContent);
    }

    #endregion    
}
