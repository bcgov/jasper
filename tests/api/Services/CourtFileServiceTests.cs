using System;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using LazyCache;
using LazyCache.Providers;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Moq;
using Scv.Api.Services;
using Xunit;

namespace tests.api.Services;

public class CourtFileServiceTests : ServiceTestBase
{
    private const string AgencyId = "123";
    private const string PartId = "part-1";
    private const string AppCode = "SCV";
    private const string FileId = "456";

    private readonly Mock<FileServicesClient> _mockFilesClient;
    private readonly CourtFileService _service;

    public CourtFileServiceTests()
    {
        var cache = new CachingService(new Lazy<ICacheProvider>(() =>
            new MemoryCacheProvider(new MemoryCache(new MemoryCacheOptions()))));

        _mockFilesClient = new Mock<FileServicesClient>(MockBehavior.Strict, this.HttpClient);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new System.Collections.Generic.Dictionary<string, string>
            {
                ["Request:ApplicationCd"] = AppCode,
                ["Request:AgencyIdentifierId"] = AgencyId,
                ["Request:PartId"] = PartId
            })
            .Build();

        _service = new CourtFileService(cache, _mockFilesClient.Object, configuration);
    }

    [Fact]
    public async Task ValidateFileExistsAsync_Fails_WhenCourtClassInvalid()
    {
        var result = await _service.ValidateFileExistsAsync("INVALID", FileId);

        Assert.False(result.Succeeded);
        Assert.Contains("Invalid CourtClassCd: INVALID", result.Errors);
    }

    [Theory]
    [InlineData("A")]
    [InlineData("Y")]
    [InlineData("T")]
    public async Task ValidateFileExistsAsync_Succeeds_WhenCriminalFileFound(string courtClass)
    {
        _mockFilesClient
            .Setup(c => c.FilesCriminalGetAsync(AgencyId, PartId, AppCode, FileId))
            .ReturnsAsync(new CriminalFileDetailResponse());

        var result = await _service.ValidateFileExistsAsync(courtClass, FileId);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateFileExistsAsync_Fails_WhenCriminalFileNotFound()
    {
        _mockFilesClient
            .Setup(c => c.FilesCriminalGetAsync(AgencyId, PartId, AppCode, FileId))
            .ReturnsAsync((CriminalFileDetailResponse)null);

        var result = await _service.ValidateFileExistsAsync("A", FileId);

        Assert.False(result.Succeeded);
        Assert.Contains($"Criminal file with id: {FileId} is not found.", result.Errors);
    }

    [Theory]
    [InlineData("C")]
    [InlineData("F")]
    [InlineData("L")]
    [InlineData("M")]
    public async Task ValidateFileExistsAsync_Succeeds_WhenCivilFileFound(string courtClass)
    {
        _mockFilesClient
            .Setup(c => c.FilesCivilGetAsync(AgencyId, PartId, AppCode, FileId))
            .ReturnsAsync(new CivilFileDetailResponse { PhysicalFileId = FileId });

        var result = await _service.ValidateFileExistsAsync(courtClass, FileId);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task ValidateFileExistsAsync_Fails_WhenCivilFileHasNoPhysicalFileId()
    {
        _mockFilesClient
            .Setup(c => c.FilesCivilGetAsync(AgencyId, PartId, AppCode, FileId))
            .ReturnsAsync(new CivilFileDetailResponse { PhysicalFileId = string.Empty });

        var result = await _service.ValidateFileExistsAsync("C", FileId);

        Assert.False(result.Succeeded);
        Assert.Contains($"Civil file with id: {FileId} is not found.", result.Errors);
    }

    [Fact]
    public async Task GetCriminalFileAsync_CachesResult()
    {
        _mockFilesClient
            .Setup(c => c.FilesCriminalGetAsync(AgencyId, PartId, AppCode, FileId))
            .ReturnsAsync(new CriminalFileDetailResponse());

        await _service.GetCriminalFileAsync(FileId);
        await _service.GetCriminalFileAsync(FileId);

        _mockFilesClient.Verify(c => c.FilesCriminalGetAsync(AgencyId, PartId, AppCode, FileId), Times.Once);
    }
}
