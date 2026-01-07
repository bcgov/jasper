using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Scv.Models;
using Scv.Models.TransitoryDocuments;
using Scv.TdApi.Infrastructure.FileSystem;
using Scv.TdApi.Infrastructure.Options;
using Scv.TdApi.Services;
using Xunit;

namespace tests.tdApi.Tests.Services;

public class SharedDriveFileServiceTests
{
    private readonly Faker _faker;
    private readonly Mock<ISmbFileSystemClient> _mockFileSystemClient;
    private readonly Mock<ILogger<SharedDriveFileService>> _mockLogger;
    private readonly Mock<IOptions<SharedDriveOptions>> _mockSharedDriveOptions;
    private readonly Mock<IOptions<CorrectionMappingOptions>> _mockCorrectionMappingOptions;
    private readonly SharedDriveOptions _defaultSharedDriveOptions;
    private readonly CorrectionMappingOptions _defaultCorrectionMappingOptions;

    public SharedDriveFileServiceTests()
    {
        _faker = new Faker();
        _mockFileSystemClient = new Mock<ISmbFileSystemClient>();
        _mockLogger = new Mock<ILogger<SharedDriveFileService>>();
        _mockSharedDriveOptions = new Mock<IOptions<SharedDriveOptions>>();
        _mockCorrectionMappingOptions = new Mock<IOptions<CorrectionMappingOptions>>();

        _defaultSharedDriveOptions = new SharedDriveOptions
        {
            DateFolderFormats = new List<string> { "yyyy/MM/dd", "yyyy-MM-dd" }
        };

        _defaultCorrectionMappingOptions = new CorrectionMappingOptions
        {
            RegionMappings = new List<CorrectionMapping>(),
            LocationMappings = new List<CorrectionMapping>()
        };

        _mockSharedDriveOptions.Setup(o => o.Value).Returns(_defaultSharedDriveOptions);
        _mockCorrectionMappingOptions.Setup(o => o.Value).Returns(_defaultCorrectionMappingOptions);
    }

    private SharedDriveFileService CreateService()
    {
        return new SharedDriveFileService(
            _mockFileSystemClient.Object,
            _mockLogger.Object,
            _mockSharedDriveOptions.Object,
            _mockCorrectionMappingOptions.Object);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenFileSystemClientIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SharedDriveFileService(
                null,
                _mockLogger.Object,
                _mockSharedDriveOptions.Object,
                _mockCorrectionMappingOptions.Object));

        exception.ParamName.Should().Be("fileSystemClient");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenLoggerIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SharedDriveFileService(
                _mockFileSystemClient.Object,
                null,
                _mockSharedDriveOptions.Object,
                _mockCorrectionMappingOptions.Object));

        exception.ParamName.Should().Be("logger");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenSharedDriveOptionsIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SharedDriveFileService(
                _mockFileSystemClient.Object,
                _mockLogger.Object,
                null,
                _mockCorrectionMappingOptions.Object));

        exception.ParamName.Should().Be("options");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenCorrectionMappingOptionsIsNull()
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() =>
            new SharedDriveFileService(
                _mockFileSystemClient.Object,
                _mockLogger.Object,
                _mockSharedDriveOptions.Object,
                null));

        exception.ParamName.Should().Be("correctionMappingOptions");
    }

    [Fact]
    public void Constructor_Succeeds_WhenAllParametersAreValid()
    {
        // Act
        var service = CreateService();

        // Assert
        service.Should().NotBeNull();
    }

    #endregion

    #region FindFilesAsync Tests

    [Fact]
    public async Task FindFilesAsync_ThrowsArgumentNullException_WhenRequestIsNull()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => service.FindFilesAsync(null));
        exception.ParamName.Should().Be("request");
    }

    [Fact]
    public async Task FindFilesAsync_ReturnsEmptyList_WhenNoFilesFound()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        var result = await service.FindFilesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task FindFilesAsync_ReturnsFiles_WhenFilesExist()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        var files = new List<SmbFileInfo>
        {
            CreateSmbFileInfo("document1.pdf", "CTR001"),
            CreateSmbFileInfo("document2.pdf", "CTR001")
        };

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), request.RoomCd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        // Act
        var result = await service.FindFilesAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].FileName.Should().Be("document1.pdf");
        result[1].FileName.Should().Be("document2.pdf");
    }

    [Fact]
    public async Task FindFilesAsync_AppliesRegionMapping_WhenMappingExists()
    {
        // Arrange
        _defaultCorrectionMappingOptions.RegionMappings = new List<CorrectionMapping>
        {
            new() { Target = "VAN", Replacement = "VancouverMapped" }
        };

        var service = CreateService();
        var request = CreateValidRequest();

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(
                It.Is<string>(path => path.Contains("VancouverMapped")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindFilesAsync_AppliesLocationMapping_WhenMappingExists()
    {
        // Arrange
        _defaultCorrectionMappingOptions.LocationMappings = new List<CorrectionMapping>
        {
            new() { Target = "4801", Replacement = "KelownaMapped" }
        };

        var service = CreateService();
        var request = CreateValidRequest();

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(
                It.Is<string>(path => path.Contains("KelownaMapped")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindFilesAsync_UsesDefaultNames_WhenNoMappingExists()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(
                It.Is<string>(path => path.Contains("Vancouver") && path.Contains("Kelowna")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindFilesAsync_IsCaseInsensitive_ForRegionMapping()
    {
        // Arrange
        _defaultCorrectionMappingOptions.RegionMappings = new List<CorrectionMapping>
        {
            new() { Target = "van", Replacement = "VancouverMapped" }
        };

        var service = CreateService();
        var request = CreateValidRequest();
        request.RegionCode = "VAN"; // Different case

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(
                It.Is<string>(path => path.Contains("VancouverMapped")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindFilesAsync_IsCaseInsensitive_ForLocationMapping()
    {
        // Arrange
        _defaultCorrectionMappingOptions.LocationMappings = new List<CorrectionMapping>
        {
            new() { Target = "KELOWNA", Replacement = "KelownaMapped" }
        };

        var service = CreateService();
        var request = CreateValidRequest();
        request.AgencyIdentifierCd = "kelowna"; // Different case

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(
                It.Is<string>(path => path.Contains("KelownaMapped")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindFilesAsync_OrdersFilesByRoomFirst()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        var files = new List<SmbFileInfo>
        {
            CreateSmbFileInfo("no-room.pdf", null),
            CreateSmbFileInfo("with-room.pdf", "CTR001")
        };

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        // Act
        var result = await service.FindFilesAsync(request);

        // Assert
        result.Should().HaveCount(2);
        result[0].MatchedRoomFolder.Should().Be("CTR001");
        result[1].MatchedRoomFolder.Should().BeNull();
    }

    [Fact]
    public async Task FindFilesAsync_OrdersFilesByRoomFolder_ThenByFileName()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        var files = new List<SmbFileInfo>
        {
            CreateSmbFileInfo("zebra.pdf", "CTR002"),
            CreateSmbFileInfo("apple.pdf", "CTR001"),
            CreateSmbFileInfo("banana.pdf", "CTR001")
        };

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(files);

        // Act
        var result = await service.FindFilesAsync(request);

        // Assert
        result.Should().HaveCount(3);
        result[0].MatchedRoomFolder.Should().Be("CTR001");
        result[0].FileName.Should().Be("apple.pdf");
        result[1].MatchedRoomFolder.Should().Be("CTR001");
        result[1].FileName.Should().Be("banana.pdf");
        result[2].MatchedRoomFolder.Should().Be("CTR002");
        result[2].FileName.Should().Be("zebra.pdf");
    }

    [Fact]
    public async Task FindFilesAsync_RemovesDuplicatesByRelativePath()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        var duplicateFile = CreateSmbFileInfo("document.pdf", "CTR001");

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo> { duplicateFile });

        // Act
        var result = await service.FindFilesAsync(request);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindFilesAsync_SearchesMultipleDateFormats()
    {
        // Arrange
        _defaultSharedDriveOptions.DateFolderFormats = new List<string>
        {
            "yyyy-MM-dd",
            "yyyyMMdd",
            "yyyy/MM/dd"
        };

        var service = CreateService();
        var request = CreateValidRequest();

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeast(3));
    }

    [Fact]
    public async Task FindFilesAsync_RemovesDuplicateDatePaths()
    {
        // Arrange
        _defaultSharedDriveOptions.DateFolderFormats = new List<string>
        {
            "yyyy-MM-dd",
            "yyyy-MM-dd" // Duplicate format
        };

        var service = CreateService();
        var request = CreateValidRequest();

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once); // Should only search once since paths are identical
    }

    [Fact]
    public async Task FindFilesAsync_PassesRoomCdToFileSystemClient()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();
        var roomCd = _faker.Random.AlphaNumeric(10);
        request.RoomCd = roomCd;

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), roomCd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(It.IsAny<string>(), roomCd, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task FindFilesAsync_CreatesCorrectFileMetadata()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        var createdDate = DateTime.UtcNow;
        var smbFile = new SmbFileInfo
        {
            FileName = "test-document.pdf",
            FullPath = "/path/to/test-document.pdf",
            Extension = ".pdf",
            SizeBytes = 12345,
            CreatedUtc = createdDate,
            RelativeDirectory = "CTR001"
        };

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo> { smbFile });

        // Act
        var result = await service.FindFilesAsync(request);

        // Assert
        result.Should().HaveCount(1);
        var dto = result[0];
        dto.FileName.Should().Be("test-document.pdf");
        dto.RelativePath.Should().Be("/path/to/test-document.pdf");
        dto.Extension.Should().Be(".pdf");
        dto.SizeBytes.Should().Be(12345);
        dto.CreatedUtc.Should().Be(createdDate);
        dto.MatchedRoomFolder.Should().Be("CTR001");
    }

    [Fact]
    public async Task FindFilesAsync_ExtractsFirstSegmentFromRelativeDirectory()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        var smbFile = new SmbFileInfo
        {
            FileName = "document.pdf",
            FullPath = "/path/document.pdf",
            Extension = ".pdf",
            SizeBytes = 1024,
            CreatedUtc = DateTime.UtcNow,
            RelativeDirectory = $"CTR001{Path.DirectorySeparatorChar}Subfolder{Path.DirectorySeparatorChar}Deep"
        };

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo> { smbFile });

        // Act
        var result = await service.FindFilesAsync(request);

        // Assert
        result.Should().HaveCount(1);
        result[0].MatchedRoomFolder.Should().Be("CTR001");
    }

    [Fact]
    public async Task FindFilesAsync_ReplacesBackslashWithForwardSlash_InDatePaths()
    {
        // Arrange
        var service = CreateService();
        var request = CreateValidRequest();

        _mockFileSystemClient
            .Setup(c => c.ListFilesAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<SmbFileInfo>());

        // Act
        await service.FindFilesAsync(request);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.ListFilesAsync(
                It.Is<string>(path => !path.Contains('\\')),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region OpenFileAsync Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task OpenFileAsync_ThrowsArgumentException_WhenPathIsNullOrWhitespace(string path)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(() => service.OpenFileAsync(path));
        exception.ParamName.Should().Be("relativePath");
    }

    [Fact]
    public async Task OpenFileAsync_ReturnsFileStreamResponse_WhenFileExists()
    {
        // Arrange
        var service = CreateService();
        var RelativePath = _faker.System.FilePath();
        var fileContent = _faker.Random.Bytes(_faker.Random.Int(100, 1000));
        var memoryStream = new MemoryStream(fileContent);

        _mockFileSystemClient
            .Setup(c => c.OpenFileAsync(RelativePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        // Act
        var result = await service.OpenFileAsync(RelativePath);

        // Assert
        result.Should().NotBeNull();
        result.Stream.Should().BeSameAs(memoryStream);
        result.FileName.Should().Be(Path.GetFileName(RelativePath));
        result.SizeBytes.Should().Be(fileContent.Length);
    }

    [Fact]
    public async Task OpenFileAsync_ThrowsIOException_WhenFileNotFound()
    {
        // Arrange
        var service = CreateService();
        var RelativePath = _faker.System.FilePath();

        _mockFileSystemClient
            .Setup(c => c.OpenFileAsync(RelativePath, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IOException("File not found"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<IOException>(() => service.OpenFileAsync(RelativePath));
        exception.Message.Should().Contain("Error opening file at path");
        exception.Message.Should().Contain(RelativePath);
        exception.InnerException.Should().NotBeNull();
        exception.InnerException.Message.Should().Be("File not found");
    }

    [Fact]
    public async Task OpenFileAsync_UsesDefaultContentType_WhenExtensionUnknown()
    {
        // Arrange
        var service = CreateService();
        var RelativePath = "/path/to/document.unknownextension";
        var memoryStream = new MemoryStream(_faker.Random.Bytes(100));

        _mockFileSystemClient
            .Setup(c => c.OpenFileAsync(RelativePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        // Act
        var result = await service.OpenFileAsync(RelativePath);

        // Assert
        result.Should().NotBeNull();
        result.ContentType.Should().Be("application/octet-stream");
    }

    [Theory]
    [InlineData("/path/to/file.pdf", "application/pdf")]
    [InlineData("/path/to/file.txt", "text/plain")]
    [InlineData("/path/to/file.jpg", "image/jpeg")]
    [InlineData("/path/to/file.jpeg", "image/jpeg")]
    [InlineData("/path/to/file.png", "image/png")]
    [InlineData("/path/to/file.gif", "image/gif")]
    [InlineData("/path/to/file.docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("/path/to/file.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    public async Task OpenFileAsync_SetsCorrectContentType_ForKnownExtensions(string filePath, string expectedContentType)
    {
        // Arrange
        var service = CreateService();
        var memoryStream = new MemoryStream(_faker.Random.Bytes(100));

        _mockFileSystemClient
            .Setup(c => c.OpenFileAsync(filePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        // Act
        var result = await service.OpenFileAsync(filePath);

        // Assert
        result.ContentType.Should().Be(expectedContentType);
    }

    [Fact]
    public async Task OpenFileAsync_ExtractsFileName_FromRelativePath()
    {
        // Arrange
        var service = CreateService();
        var RelativePath = "/very/long/path/to/my/document.pdf";
        var memoryStream = new MemoryStream(_faker.Random.Bytes(100));

        _mockFileSystemClient
            .Setup(c => c.OpenFileAsync(RelativePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        // Act
        var result = await service.OpenFileAsync(RelativePath);

        // Assert
        result.FileName.Should().Be("document.pdf");
    }

    [Fact]
    public async Task OpenFileAsync_HandlesFileNameWithSpaces()
    {
        // Arrange
        var service = CreateService();
        var RelativePath = "/path/to/my document with spaces.pdf";
        var memoryStream = new MemoryStream(_faker.Random.Bytes(100));

        _mockFileSystemClient
            .Setup(c => c.OpenFileAsync(RelativePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        // Act
        var result = await service.OpenFileAsync(RelativePath);

        // Assert
        result.FileName.Should().Be("my document with spaces.pdf");
    }

    [Fact]
    public async Task OpenFileAsync_PassesCorrectPath_ToFileSystemClient()
    {
        // Arrange
        var service = CreateService();
        var RelativePath = _faker.System.FilePath();
        var memoryStream = new MemoryStream(_faker.Random.Bytes(100));

        _mockFileSystemClient
            .Setup(c => c.OpenFileAsync(RelativePath, It.IsAny<CancellationToken>()))
            .ReturnsAsync(memoryStream);

        // Act
        await service.OpenFileAsync(RelativePath);

        // Assert
        _mockFileSystemClient.Verify(
            c => c.OpenFileAsync(RelativePath, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    private TransitoryDocumentSearchRequest CreateValidRequest()
    {
        return new TransitoryDocumentSearchRequest
        {
            RegionCode = "VAN",
            RegionName = "Vancouver",
            AgencyIdentifierCd = "4801",
            LocationShortName = "Kelowna",
            RoomCd = "CTR001",
            Date = DateOnly.FromDateTime(_faker.Date.Recent())
        };
    }

    private SmbFileInfo CreateSmbFileInfo(string fileName, string relativeDirectory)
    {
        var extension = Path.GetExtension(fileName);
        return new SmbFileInfo
        {
            FileName = fileName,
            FullPath = $"/path/to/{fileName}",
            Extension = extension,
            SizeBytes = _faker.Random.Long(1000, 1000000),
            CreatedUtc = _faker.Date.Recent(),
            RelativeDirectory = relativeDirectory
        };
    }

    #endregion
}