using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using PCSSCommon.Clients.ReportServices;
using PCSSCommon.Clients.SearchDateServices;
using Scv.Api.Documents;
using Scv.Api.Infrastructure;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Models;
using Scv.Api.Models.CourtList;
using Scv.Api.Models.Document;
using Scv.Api.Services;
using Scv.Db.Contants;
using Scv.Db.Models;
using Xunit;

namespace tests.api.Services;
public class CourtListServiceTests : ServiceTestBase
{
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Faker _faker;
    private readonly IMapper _mapper;
    private readonly Mock<IBinderService> _binderService;
    private readonly Mock<IDocumentConverter> _documentConverter;
    private readonly Mock<IDocumentMerger> _documentMerger;
    private readonly Dictionary<string, string> _mockLabels;

    public CourtListServiceTests()
    {
        _faker = new Faker();

        // IConfiguration setup
        _mockConfig = new Mock<IConfiguration>();

        var mockFileExpirySection = new Mock<IConfigurationSection>();
        mockFileExpirySection.Setup(s => s.Value).Returns(_faker.Random.Number().ToString());

        var mockRefreshHoursSection = new Mock<IConfigurationSection>();
        mockRefreshHoursSection.Setup(s => s.Value).Returns("12");

        _mockConfig.Setup(c => c.GetSection("Caching:FileExpiryMinutes")).Returns(mockFileExpirySection.Object);
        _mockConfig.Setup(c => c.GetSection("KEY_DOCS_BINDER_REFRESH_HOURS")).Returns(mockRefreshHoursSection.Object);

        // IMapper setup
        var config = new TypeAdapterConfig();
        config.Apply(new BinderMapping());
        _mapper = new Mapper(config);

        _binderService = new Mock<IBinderService>();
        _documentConverter = new Mock<IDocumentConverter>();
        _documentMerger = new Mock<IDocumentMerger>();

        _mockLabels = new Dictionary<string, string>
        {
            { LabelConstants.PHYSICAL_FILE_ID, _faker.Random.Int().ToString() },
            { LabelConstants.PARTICIPANT_ID, _faker.Random.Float().ToString() },
            { LabelConstants.APPEARANCE_ID, _faker.Random.Float().ToString() },
            { LabelConstants.COURT_CLASS_CD, CourtClassCd.A.ToString() }
        };


    }

    private (
        CourtListService courtListService,
        Mock<ReportServicesClient> mockReportClient,
        Mock<FileServicesClient> mockFileClient
        ) SetupCourtListService()
    {
        // Setup Service Clients
        var mockFileClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);
        var mockSearchDateClient = new Mock<SearchDateClient>(MockBehavior.Strict, this.HttpClient);
        var mockReportClient = new Mock<ReportServicesClient>(MockBehavior.Strict, this.HttpClient);

        // Setup Services
        var mockLocationService = new Mock<LocationService>(MockBehavior.Strict, this.HttpClient);
        var mockLookupService = new Mock<LookupService>(MockBehavior.Strict, this.HttpClient);

        // Setup cache
        var cachingService = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        // Setup ClaimsPrincipal
        var identity = new ClaimsIdentity([], "mock");


        var courtListService = new CourtListService(
            _mockConfig.Object,
            new Mock<ILogger<CourtListService>>().Object,
            mockFileClient.Object,
            _mapper,
            null,
            null,
            mockSearchDateClient.Object,
            mockReportClient.Object,
            cachingService,
            new ClaimsPrincipal(identity),
            _binderService.Object,
            _documentConverter.Object,
            _documentMerger.Object);

        return (
            courtListService,
            mockReportClient,
            mockFileClient
        );
    }

    [Fact]
    public async Task GenerateReport_ShouldReturnStream()
    {
        var (courtListService, mockReportClient, _) = SetupCourtListService();
        var fakeContentDisposition = $"inline; filename={Path.GetRandomFileName()}";

        mockReportClient
            .Setup(r => r.GetCourtListReportAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .ReturnsAsync((new MemoryStream(), fakeContentDisposition));

        var (stream, contentDisposition) = await courtListService.GenerateReportAsync(new CourtListReportRequest());

        Assert.NotNull(stream);
        Assert.Equal(fakeContentDisposition, contentDisposition);
        mockReportClient
            .Verify(r => r
                .GetCourtListReportAsync(
                    It.IsAny<string>(),
                    It.IsAny<DateTimeOffset>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()),
                Times.Once());
    }

    [Fact]
    public async Task ProcessCourtListDocumentBundle_ShouldReturnSuccess_WhenBinderExists()
    {
        var request = new CourtListDocumentBundleRequest
        {
            Appearances =
            [
                new() {

                    FileId = _mockLabels[LabelConstants.PHYSICAL_FILE_ID],
                    ParticipantId = _mockLabels[LabelConstants.PARTICIPANT_ID],
                    AppearanceId = _mockLabels[LabelConstants.APPEARANCE_ID],
                    CourtClassCd = _mockLabels[LabelConstants.COURT_CLASS_CD],
                }
            ]
        };

        _binderService.Setup(b => b.GetByLabels(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(OperationResult<List<BinderDto>>.Success(
                [
                    new()
                    {
                        UpdatedDate = DateTime.UtcNow,
                        Labels = _mockLabels,
                        Documents =
                        [
                            new()
                            {
                                DocumentType = DocumentType.File,
                                DocumentId = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(_faker.Random.AlphaNumeric(5)))
                            }
                        ]
                    }
                ]));

        _documentMerger
            .Setup(dm => dm.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(new PdfDocumentResponse());

        var (courtListService, _, _) = SetupCourtListService();

        var result = await courtListService.ProcessCourtListDocumentBundle(request);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _binderService.Verify(b => b.GetByLabels(It.IsAny<Dictionary<string, string>>()), Times.Once);
        _documentMerger.Verify(dm => dm.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCourtListDocumentBundle_ShouldReturnSuccess_WhenBinderDoesNotExists()
    {
        var criminalFile = new CriminalFileContent
        {
            AccusedFile =
            [
                new()
                {
                    MdocJustinNo = _mockLabels[LabelConstants.PHYSICAL_FILE_ID],
                    PartId = _mockLabels[LabelConstants.PARTICIPANT_ID],
                }
            ]
        };

        var request = new CourtListDocumentBundleRequest
        {
            Appearances =
            [
                new() {

                    FileId = _mockLabels[LabelConstants.PHYSICAL_FILE_ID],
                    ParticipantId = _mockLabels[LabelConstants.PARTICIPANT_ID],
                    AppearanceId = _mockLabels[LabelConstants.APPEARANCE_ID],
                    CourtClassCd = _mockLabels[LabelConstants.COURT_CLASS_CD],
                }
            ]
        };

        _binderService.Setup(b => b.GetByLabels(It.IsAny<Dictionary<string, string>>()))
                .ReturnsAsync(OperationResult<List<BinderDto>>.Success([]));
        _binderService.Setup(b => b.AddAsync(It.IsAny<BinderDto>()))
                .ReturnsAsync(OperationResult<BinderDto>.Success(new BinderDto
                {
                    Documents =
                    [
                        new()
                        {
                            DocumentType = DocumentType.ROP
                        }
                    ]
                }));
        _documentMerger
            .Setup(dm => dm.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()))
            .ReturnsAsync(new PdfDocumentResponse());
        _documentConverter
            .Setup(dc => dc.GetCriminalDocuments(criminalFile.AccusedFile.First()))
            .ReturnsAsync([
                new() {
                    Category = DocumentCategory.ROP
                }]);

        var (courtListService, _, mockFileClient) = SetupCourtListService();

        mockFileClient
            .Setup(f => f.FilesCriminalFilecontentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                null,
                null,
                null,
                null,
                _mockLabels[LabelConstants.PHYSICAL_FILE_ID]))
            .ReturnsAsync(criminalFile);

        var result = await courtListService.ProcessCourtListDocumentBundle(request);

        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        _binderService.Verify(b => b.GetByLabels(It.IsAny<Dictionary<string, string>>()), Times.Once);
        _documentMerger.Verify(dm => dm.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()), Times.Once);
        _documentConverter.Verify(dc => dc.GetCriminalDocuments(It.IsAny<CfcAccusedFile>()), Times.Once);
    }

    [Fact]
    public async Task ProcessCourtListDocumentBundle_ShouldReturnFailed_WhenRequestContainsUnsupportedCourtClass()
    {
        _mockLabels[LabelConstants.COURT_CLASS_CD] = CourtClassCd.O.ToString();
        var criminalFile = new CriminalFileContent
        {
            AccusedFile =
            [
                new()
                {
                    MdocJustinNo = _mockLabels[LabelConstants.PHYSICAL_FILE_ID],
                    PartId = _mockLabels[LabelConstants.PARTICIPANT_ID],
                }
            ]
        };

        var request = new CourtListDocumentBundleRequest
        {
            Appearances =
            [
                new() {

                    FileId = _mockLabels[LabelConstants.PHYSICAL_FILE_ID],
                    ParticipantId = _mockLabels[LabelConstants.PARTICIPANT_ID],
                    AppearanceId = _mockLabels[LabelConstants.APPEARANCE_ID],
                    CourtClassCd = _mockLabels[LabelConstants.COURT_CLASS_CD],
                }
            ]
        };


        var (courtListService, _, _) = SetupCourtListService();

        var result = await courtListService.ProcessCourtListDocumentBundle(request);

        Assert.NotNull(result);
        Assert.False(result.Succeeded);
        _binderService.Verify(b => b.GetByLabels(It.IsAny<Dictionary<string, string>>()), Times.Never);
        _documentMerger.Verify(dm => dm.MergeDocuments(It.IsAny<PdfDocumentRequest[]>()), Times.Never);
        _documentConverter.Verify(dc => dc.GetCriminalDocuments(It.IsAny<CfcAccusedFile>()), Times.Never);
    }
}