using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using MapsterMapper;
using Moq;
using Scv.Models.Civil.Detail;
using Scv.Api.Services.Files;
using tests.api.Fixtures;
using Xunit;

namespace tests.api.Services;

[Collection("ServiceFixture")]
public class CivilFilesServiceTests(FilesServiceFixture filesServiceFixture)
{
    private const string FileId = "2506";
    private readonly FilesServiceFixture _filesServiceFixture = filesServiceFixture;

    [Fact]
    public async Task GetDocumentsByIds_Should_ReturnEmpty_When_FileIdOrDocumentIdsAreInvalid()
    {
        // Arrange
        _filesServiceFixture.Reset();
        var service = CreateService(new Mock<IMapper>(MockBehavior.Strict).Object);

        // Act
        var resultWithMissingFileId = await service.GetDocumentsByIds("", ["doc-1"]);
        var resultWithMissingDocumentIds = await service.GetDocumentsByIds(FileId, []);

        // Assert
        Assert.Empty(resultWithMissingFileId);
        Assert.Empty(resultWithMissingDocumentIds);

        _filesServiceFixture.MockFileServicesClient.Verify(
            s => s.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task GetDocumentsByIds_Should_DedupeOtherDocs_ByCivilDocumentId()
    {
        // Arrange
        _filesServiceFixture.Reset();

        var mappedDetail = new RedactedCivilFileDetailResponse
        {
            Document =
            [
                new CivilDocument
                {
                    CivilDocumentId = "doc-1",
                    ImageId = "img-1",
                    DocumentTypeCd = "AFF",
                    Issue = []
                },
                new CivilDocument
                {
                    CivilDocumentId = "doc-1",
                    ImageId = "img-2",
                    DocumentTypeCd = "AFF",
                    Issue = []
                },
                new CivilDocument
                {
                    CivilDocumentId = "doc-2",
                    ImageId = "img-3",
                    DocumentTypeCd = "AFF",
                    Issue = []
                }
            ],
            ReferenceDocument = []
        };

        var mapper = new Mock<IMapper>(MockBehavior.Strict);
        mapper
            .Setup(m => m.Map<RedactedCivilFileDetailResponse>(It.IsAny<CivilFileDetailResponse>()))
            .Returns(mappedDetail);

        _filesServiceFixture.MockFileServicesClient
            .Setup(s => s.FilesCivilGetAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                FileId))
            .ReturnsAsync(new CivilFileDetailResponse
            {
                Appearance = [],
                Document = [],
                ReferenceDocument = []
            });

        _filesServiceFixture.MockFileServicesClient
            .Setup(s => s.FilesCivilFilecontentAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                FileId))
            .ReturnsAsync(new CivilFileContent
            {
                CivilFile =
                [
                    new CvfcCivilFile
                    {
                        PhysicalFileID = FileId,
                        Document = []
                    }
                ]
            });

        _filesServiceFixture.MockLookupService
            .Setup(s => s.GetDocumentCategory(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("AFFIDAVITS");

        _filesServiceFixture.MockLookupService
            .Setup(s => s.GetDocumentDescriptionAsync(It.IsAny<string>()))
            .ReturnsAsync("Affidavit");

        var service = CreateService(mapper.Object);

        // Act
        var result = await service.GetDocumentsByIds(FileId, ["doc-1", "doc-2"]);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Single(result, d => d.CivilDocumentId == "doc-1");
        Assert.Single(result, d => d.CivilDocumentId == "doc-2");

        _filesServiceFixture.MockLookupService.Verify(
            s => s.GetDocumentCategory(It.IsAny<string>(), It.IsAny<string>()),
            Times.Exactly(2));
        _filesServiceFixture.MockLookupService.Verify(
            s => s.GetDocumentDescriptionAsync(It.IsAny<string>()),
            Times.Exactly(2));
    }

    private CivilFilesService CreateService(IMapper mapper)
    {
        return new CivilFilesService(
            _filesServiceFixture.MockConfiguration.Object,
            _filesServiceFixture.MockFileServicesClient.Object,
            mapper,
            _filesServiceFixture.MockLookupService.Object,
            _filesServiceFixture.MockLocationService.Object,
            _filesServiceFixture.Cache,
            _filesServiceFixture.Principal,
            _filesServiceFixture.MockCivilLogger.Object);
    }
}
