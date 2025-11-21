using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using LazyCache;
using LazyCache.Providers;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Scv.Api.Infrastructure.Mappings;
using Scv.Api.Services;
using Scv.Core.Helpers.Exceptions;
using TDCommon.Clients.DocumentsServices;
using Xunit;
using JCRegion = JCCommon.Clients.LocationServices.Region;

namespace tests.api.Services;

public class TransitoryDocumentsServiceTests : ServiceTestBase
{
    private readonly Mock<ILogger<TransitoryDocumentsService>> _mockLogger;
    private readonly Mock<ILogger<LocationService>> _mockLocationLogger;
    private readonly Mock<IConfiguration> _mockConfig;
    private readonly Faker _faker;
    private readonly IMapper _mapper;
    private readonly IAppCache _cache;

    public TransitoryDocumentsServiceTests()
    {
        _faker = new Faker();
        _mockLogger = new Mock<ILogger<TransitoryDocumentsService>>();
        _mockLocationLogger = new Mock<ILogger<LocationService>>();
        _mockConfig = new Mock<IConfiguration>();

        var mockCachingSection = new Mock<IConfigurationSection>();
        mockCachingSection.Setup(s => s.Value).Returns("60");
        _mockConfig.Setup(c => c.GetSection("Caching:LocationExpiryMinutes")).Returns(mockCachingSection.Object);
        _mockConfig.Setup(c => c.GetSection("Caching:FileExpiryMinutes")).Returns(mockCachingSection.Object);

        var config = new TypeAdapterConfig();
        config.Apply(new TransitoryDocumentsMapping());
        config.Apply(new LocationMapping());
        _mapper = new Mapper(config);

        _cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));
    }

    private (TransitoryDocumentsService service,
        Mock<ITransitoryDocumentsClientService> mockTdClient,
        Mock<ILocationService> mockLocationService,
        Mock<IKeycloakTokenService> mockKeycloakTokenService) SetupService(
            List<TDCommon.Clients.DocumentsServices.FileMetadataDto> searchResults = null,
            Scv.Models.Location.Location location = null,
            JCRegion region = null)
    {
        var mockTdClient = new Mock<ITransitoryDocumentsClientService>();
        var mockKeycloakTokenService = new Mock<IKeycloakTokenService>();
        var mockLocationService = new Mock<ILocationService>();

        var bearerToken = _faker.Random.AlphaNumeric(50);
        mockKeycloakTokenService
            .Setup(k => k.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(bearerToken);

        // SetBearerToken can now be mocked since it's part of the interface
        mockTdClient.Setup(c => c.SetBearerToken(It.IsAny<string>()));

        if (searchResults != null)
        {
            mockTdClient
                .Setup(c => c.SearchAsync(
                    It.IsAny<TransitoryDocumentSearchRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(searchResults);
        }

        if (location != null)
        {
            mockLocationService
                .Setup(l => l.GetLocations(It.IsAny<bool>()))
                .ReturnsAsync(new List<Scv.Models.Location.Location> { location });
        }

        if (region != null)
        {
            mockLocationService
                .Setup(l => l.GetRegion(It.IsAny<string>()))
                .ReturnsAsync(region);
        }

        var service = new TransitoryDocumentsService(
            _mockLogger.Object,
            mockTdClient.Object,
            mockLocationService.Object,
            mockKeycloakTokenService.Object,
            _mapper);

        return (service, mockTdClient, mockLocationService, mockKeycloakTokenService);
    }

    #region ListSharedDocuments Tests

    [Fact]
    public async Task ListSharedDocuments_ShouldReturnMappedResults_WhenSearchSucceeds()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = _faker.Random.AlphaNumeric(5);
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var region = new JCRegion
        {
            RegionId = _faker.Random.Int(1, 10),
            RegionName = _faker.Address.State(),
            RegionLocations = new List<string>()
        };

        var tdResults = new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>
        {
            new TDCommon.Clients.DocumentsServices.FileMetadataDto
            {
                FileName = "test.pdf",
                Extension = ".pdf",
                SizeBytes = 1024,
                CreatedUtc = DateTimeOffset.UtcNow,
                RelativePath = "path/to/test.pdf",
                MatchedRoomFolder = roomCode
            }
        };

        var (service, mockTdClient, _, _) = SetupService(tdResults, location, region);

        // Act
        var result = await service.ListSharedDocuments(locationId, roomCode, date);

        // Assert
        Assert.NotNull(result);
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal("test.pdf", resultList[0].FileName);
        Assert.Equal(".pdf", resultList[0].Extension);
        Assert.Equal(1024, resultList[0].SizeBytes);
        Assert.Equal("path/to/test.pdf", resultList[0].RelativePath);
        Assert.Equal(roomCode, resultList[0].MatchedRoomFolder);

        mockTdClient.Verify(c => c.SearchAsync(
            It.Is<TransitoryDocumentSearchRequest>(r =>
                r.RoomCd == roomCode &&
                r.AgencyIdentifierCd == location.AgencyIdentifierCd &&
                r.LocationShortName == location.ShortName &&
                r.RegionCode == region.RegionId.ToString() &&
                r.RegionName == region.RegionName),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListSharedDocuments_ShouldMapDateTimeOffsetToDateTime_Correctly()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = _faker.Random.AlphaNumeric(5);
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var region = new JCRegion
        {
            RegionId = _faker.Random.Int(1, 10),
            RegionName = _faker.Address.State(),
            RegionLocations = new List<string>()
        };

        var expectedDateTime = new DateTimeOffset(2024, 1, 15, 10, 30, 0, TimeSpan.Zero);
        var tdResults = new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>
        {
            new TDCommon.Clients.DocumentsServices.FileMetadataDto
            {
                FileName = "test.pdf",
                Extension = ".pdf",
                SizeBytes = 1024,
                CreatedUtc = expectedDateTime,
                RelativePath = "path/to/test.pdf",
                MatchedRoomFolder = null
            }
        };

        var (service, _, _, _) = SetupService(tdResults, location, region);

        // Act
        var result = await service.ListSharedDocuments(locationId, roomCode, date);

        // Assert
        var resultList = result.ToList();
        Assert.Single(resultList);
        Assert.Equal(expectedDateTime.UtcDateTime, resultList[0].CreatedUtc);
        Assert.Equal(DateTimeKind.Utc, resultList[0].CreatedUtc.Kind);
    }

    [Fact]
    public async Task ListSharedDocuments_ShouldReturnEmptyList_WhenNoResultsFound()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = _faker.Random.AlphaNumeric(5);
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var region = new JCRegion
        {
            RegionId = _faker.Random.Int(1, 10),
            RegionName = _faker.Address.State(),
            RegionLocations = new List<string>()
        };

        var tdResults = new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>();

        var (service, _, _, _) = SetupService(tdResults, location, region);

        // Act
        var result = await service.ListSharedDocuments(locationId, roomCode, date);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ListSharedDocuments_ShouldThrowBadRequestException_WhenLocationNotFound()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");

        var (service, _, mockLocationService, _) = SetupService();

        mockLocationService
            .Setup(l => l.GetLocations(It.IsAny<bool>()))
            .ReturnsAsync(new List<Scv.Models.Location.Location>());

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.ListSharedDocuments(locationId, roomCode, date));

        Assert.Equal("location not found.", exception.Message);
    }

    [Fact]
    public async Task ListSharedDocuments_ShouldThrowBadRequestException_WhenLocationShortNameIsNull()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = null;
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var (service, _, mockLocationService, _) = SetupService();

        mockLocationService
            .Setup(l => l.GetLocations(It.IsAny<bool>()))
            .ReturnsAsync(new List<Scv.Models.Location.Location> { location });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.ListSharedDocuments(locationId, roomCode, date));

        Assert.Equal("location not found.", exception.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task ListSharedDocuments_ShouldThrowBadRequestException_WhenRegionNameIsNullOrEmpty(string regionName)
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = _faker.Random.AlphaNumeric(5);
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var region = new JCRegion
        {
            RegionId = _faker.Random.Int(1, 10),
            RegionName = regionName,
            RegionLocations = new List<string>()
        };

        var (service, _, mockLocationService, _) = SetupService();

        mockLocationService
            .Setup(l => l.GetLocations(It.IsAny<bool>()))
            .ReturnsAsync(new List<Scv.Models.Location.Location> { location });

        mockLocationService
            .Setup(l => l.GetRegion(It.IsAny<string>()))
            .ReturnsAsync(region);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.ListSharedDocuments(locationId, roomCode, date));

        Assert.Equal("Region not found.", exception.Message);
    }

    [Theory]
    [InlineData("invalid-date")]
    [InlineData("2024-13-01")]
    [InlineData("not a date")]
    public async Task ListSharedDocuments_ShouldThrowBadRequestException_WhenDateFormatIsInvalid(string invalidDate)
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = _faker.Random.AlphaNumeric(5);
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var region = new JCRegion
        {
            RegionId = _faker.Random.Int(1, 10),
            RegionName = _faker.Address.State(),
            RegionLocations = new List<string>()
        };

        var (service, _, mockLocationService, _) = SetupService();

        mockLocationService
            .Setup(l => l.GetLocations(It.IsAny<bool>()))
            .ReturnsAsync(new List<Scv.Models.Location.Location> { location });

        mockLocationService
            .Setup(l => l.GetRegion(It.IsAny<string>()))
            .ReturnsAsync(region);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() =>
            service.ListSharedDocuments(locationId, roomCode, invalidDate));

        Assert.Equal("Invalid date format.", exception.Message);
    }

    [Fact]
    public async Task ListSharedDocuments_ShouldSetBearerToken_BeforeCallingApi()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");
        var expectedToken = _faker.Random.AlphaNumeric(50);

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = _faker.Random.AlphaNumeric(5);
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var region = new JCRegion
        {
            RegionId = _faker.Random.Int(1, 10),
            RegionName = _faker.Address.State(),
            RegionLocations = new List<string>()
        };

        var tdResults = new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>();

        var (service, mockTdClient, mockLocationService, mockKeycloakService) = SetupService();

        mockLocationService
            .Setup(l => l.GetLocations(It.IsAny<bool>()))
            .ReturnsAsync(new List<Scv.Models.Location.Location> { location });

        mockLocationService
            .Setup(l => l.GetRegion(It.IsAny<string>()))
            .ReturnsAsync(region);

        mockKeycloakService
            .Setup(k => k.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        mockTdClient
            .Setup(c => c.SearchAsync(
                It.IsAny<TransitoryDocumentSearchRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tdResults);

        // Act
        await service.ListSharedDocuments(locationId, roomCode, date);

        // Assert
        mockKeycloakService.Verify(k => k.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockTdClient.Verify(c => c.SetBearerToken(expectedToken), Times.Once);
    }

    [Fact]
    public async Task ListSharedDocuments_ShouldHandleMultipleDocuments()
    {
        // Arrange
        var locationId = _faker.Random.AlphaNumeric(5);
        var roomCode = _faker.Random.AlphaNumeric(3);
        var date = _faker.Date.Recent().ToString("yyyy-MM-dd");

        var location = Scv.Models.Location.Location.Create(
            _faker.Address.City(),
            locationId,
            locationId,
            true,
            new List<Scv.Models.Location.CourtRoom>());
        location.ShortName = _faker.Random.AlphaNumeric(5);
        location.AgencyIdentifierCd = _faker.Random.AlphaNumeric(3);

        var region = new JCRegion
        {
            RegionId = _faker.Random.Int(1, 10),
            RegionName = _faker.Address.State(),
            RegionLocations = new List<string>()
        };

        var tdResults = new List<TDCommon.Clients.DocumentsServices.FileMetadataDto>
        {
            new TDCommon.Clients.DocumentsServices.FileMetadataDto
            {
                FileName = "test1.pdf",
                Extension = ".pdf",
                SizeBytes = 1024,
                CreatedUtc = DateTimeOffset.UtcNow,
                RelativePath = "path/to/test1.pdf",
                MatchedRoomFolder = roomCode
            },
            new TDCommon.Clients.DocumentsServices.FileMetadataDto
            {
                FileName = "test2.docx",
                Extension = ".docx",
                SizeBytes = 2048,
                CreatedUtc = DateTimeOffset.UtcNow.AddHours(-1),
                RelativePath = "path/to/test2.docx",
                MatchedRoomFolder = null
            }
        };

        var (service, _, _, _) = SetupService(tdResults, location, region);

        // Act
        var result = await service.ListSharedDocuments(locationId, roomCode, date);

        // Assert
        var resultList = result.ToList();
        Assert.Equal(2, resultList.Count);
        Assert.Equal("test1.pdf", resultList[0].FileName);
        Assert.Equal("test2.docx", resultList[1].FileName);
    }

    #endregion

    #region DownloadFile Tests

    [Fact]
    public async Task DownloadFile_ShouldReturnFileStreamResponse_WhenFileExists()
    {
        // Arrange
        var path = "/path/to/file.pdf";
        var fileName = "file.pdf";
        var contentType = "application/pdf";
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Disposition", new[] { $"attachment; filename=\"{fileName}\"" } },
            { "Content-Type", new[] { contentType } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await service.DownloadFile(path);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.FileName);
        Assert.Equal(contentType, result.ContentType);
        Assert.Equal(stream, result.Stream);
    }

    [Fact]
    public async Task DownloadFile_ShouldExtractFileName_FromContentDispositionHeader()
    {
        // Arrange
        var path = "/path/to/file.pdf";
        var expectedFileName = "test-document.pdf";
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Disposition", new[] { $"attachment; filename=\"{expectedFileName}\"" } },
            { "Content-Type", new[] { "application/pdf" } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await service.DownloadFile(path);

        // Assert
        Assert.Equal(expectedFileName, result.FileName);
    }

    [Fact]
    public async Task DownloadFile_ShouldHandleUtf8EncodedFileName()
    {
        // Arrange
        var path = "/path/to/file.pdf";
        var expectedFileName = "test file with spaces.pdf";
        var encodedFileName = Uri.EscapeDataString(expectedFileName);
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Disposition", new[] { $"attachment; filename*=UTF-8''{encodedFileName}" } },
            { "Content-Type", new[] { "application/pdf" } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await service.DownloadFile(path);

        // Assert
        Assert.Equal(expectedFileName, result.FileName);
    }

    [Fact]
    public async Task DownloadFile_ShouldFallbackToPathFileName_WhenNoContentDispositionHeader()
    {
        // Arrange
        var path = "/path/to/fallback-file.pdf";
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Type", new[] { "application/pdf" } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await service.DownloadFile(path);

        // Assert
        Assert.Equal("fallback-file.pdf", result.FileName);
    }

    [Fact]
    public async Task DownloadFile_ShouldExtractContentType_FromHeaders()
    {
        // Arrange
        var path = "/path/to/file.docx";
        var expectedContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Disposition", new[] { "attachment; filename=\"file.docx\"" } },
            { "Content-Type", new[] { expectedContentType } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await service.DownloadFile(path);

        // Assert
        Assert.Equal(expectedContentType, result.ContentType);
    }

    [Fact]
    public async Task DownloadFile_ShouldStripCharsetFromContentType()
    {
        // Arrange
        var path = "/path/to/file.txt";
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Disposition", new[] { "attachment; filename=\"file.txt\"" } },
            { "Content-Type", new[] { "text/plain; charset=utf-8" } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await service.DownloadFile(path);

        // Assert
        Assert.Equal("text/plain", result.ContentType);
    }

    [Fact]
    public async Task DownloadFile_ShouldDefaultToOctetStream_WhenNoContentTypeHeader()
    {
        // Arrange
        var path = "/path/to/file.bin";
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Disposition", new[] { "attachment; filename=\"file.bin\"" } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        var result = await service.DownloadFile(path);

        // Assert
        Assert.Equal("application/octet-stream", result.ContentType);
    }

    [Theory]
    [InlineData(400, typeof(BadRequestException), "Invalid file path")]
    [InlineData(401, typeof(NotAuthorizedException), "Unauthorized access to file")]
    [InlineData(403, typeof(NotAuthorizedException), "Access to file is forbidden")]
    [InlineData(404, typeof(NotFoundException), "File not found")]
    public async Task DownloadFile_ShouldThrowAppropriateException_WhenApiReturnsError(
        int statusCode, Type exceptionType, string expectedMessagePart)
    {
        // Arrange
        var path = "/path/to/file.pdf";
        var errorMessage = _faker.Lorem.Sentence();

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException<string>(
                errorMessage,
                statusCode,
                errorMessage,
                new Dictionary<string, IEnumerable<string>>(),
                errorMessage,
                null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync(exceptionType, () => service.DownloadFile(path));
        Assert.Contains(expectedMessagePart, exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DownloadFile_ShouldThrowBadRequestException_ForUnhandledStatusCodes()
    {
        // Arrange
        var path = "/path/to/file.pdf";
        var errorMessage = _faker.Lorem.Sentence();

        var (service, mockTdClient, _, _) = SetupService();

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ApiException<string>(
                errorMessage,
                500,
                errorMessage,
                new Dictionary<string, IEnumerable<string>>(),
                errorMessage,
                null));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<BadRequestException>(() => service.DownloadFile(path));
        Assert.Contains("Failed to download file", exception.Message);
    }

    [Fact]
    public async Task DownloadFile_ShouldSetBearerToken_BeforeDownloading()
    {
        // Arrange
        var path = "/path/to/file.pdf";
        var expectedToken = _faker.Random.AlphaNumeric(50);
        var stream = new System.IO.MemoryStream(_faker.Random.Bytes(100));
        var headers = new Dictionary<string, IEnumerable<string>>
        {
            { "Content-Disposition", new[] { "attachment; filename=\"file.pdf\"" } },
            { "Content-Type", new[] { "application/pdf" } }
        };

        var fileResponse = new FileResponse(200, headers, stream, null, null);

        var (service, mockTdClient, _, mockKeycloakService) = SetupService();

        mockKeycloakService
            .Setup(k => k.GetAccessTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedToken);

        mockTdClient
            .Setup(c => c.ContentAsync(path, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fileResponse);

        // Act
        await service.DownloadFile(path);

        // Assert
        mockKeycloakService.Verify(k => k.GetAccessTokenAsync(It.IsAny<CancellationToken>()), Times.Once);
        mockTdClient.Verify(c => c.SetBearerToken(expectedToken), Times.Once);
    }

    #endregion
}
