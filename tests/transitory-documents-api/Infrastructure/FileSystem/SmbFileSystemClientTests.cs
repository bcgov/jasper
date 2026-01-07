using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bogus;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Retry;
using Scv.TdApi.Infrastructure.FileSystem;
using Scv.TdApi.Infrastructure.Options;
using SMBLibrary;
using SMBLibrary.Client;
using Xunit;

namespace tests.tdApi.Tests.Infrastructure.FileSystem
{
    public class SmbFileSystemClientTests
    {
        private readonly Faker _faker;
        private readonly Mock<ILogger<SmbFileSystemClient>> _mockLogger;
        private readonly Mock<IOptionsMonitor<SharedDriveOptions>> _mockOptions;
        private readonly Mock<ISmbClientFactory> _mockClientFactory;
        private readonly Mock<ISmbClient> _mockSmbClient;
        private readonly Mock<ISMBFileStore> _mockFileStore;
        private readonly AsyncRetryPolicy _noRetryPolicy;
        private readonly SharedDriveOptions _defaultOptions;

        public SmbFileSystemClientTests()
        {
            _faker = new Faker();
            _mockLogger = new Mock<ILogger<SmbFileSystemClient>>();
            _mockOptions = new Mock<IOptionsMonitor<SharedDriveOptions>>();
            _mockClientFactory = new Mock<ISmbClientFactory>();
            _mockSmbClient = new Mock<ISmbClient>();
            _mockFileStore = new Mock<ISMBFileStore>();

            // Create a no-retry policy for testing
            _noRetryPolicy = Policy
                .Handle<IOException>()
                .WaitAndRetryAsync(0, _ => TimeSpan.Zero);

            _defaultOptions = new SharedDriveOptions
            {
                BasePath = "TestBase",
                SmbServer = "test-server",
                SmbShareName = "test-share",
                SmbUsername = "test-user",
                SmbPassword = "test-pass",
                SmbDomain = "test-domain",
                DateFolderFormats = new List<string> { "yyyy-MM-dd" },
                MaxRetryAttempts = 0,
                InitialRetryDelayMs = 0,
                FileBufferSize = 65536,
                DirectoryListingMaxConcurrency = 4
            };

            _mockOptions.Setup(o => o.CurrentValue).Returns(_defaultOptions);
            _mockClientFactory.Setup(f => f.CreateClient()).Returns(_mockSmbClient.Object);
        }

        #region ListFilesAsync tests

        [Fact]
        public async Task ListFilesAsync_Returns_Failure_When_Connection_Fails()
        {
            // Arrange
            _mockSmbClient
                .Setup(c => c.Connect(It.IsAny<string>(), It.IsAny<SMBTransportType>()))
                .Returns(false);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.ListFilesAsync(_faker.System.DirectoryPath()));

            exception.Message.Should().Contain("Failed to connect to SMB server");
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Failure_When_Login_Fails()
        {
            // Arrange
            _mockSmbClient
                .Setup(c => c.Connect(It.IsAny<string>(), It.IsAny<SMBTransportType>()))
                .Returns(true);

            _mockSmbClient
                .Setup(c => c.Login(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(NTStatus.STATUS_LOGON_FAILURE);

            _mockSmbClient
                .Setup(c => c.IsConnected)
                .Returns(true);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.ListFilesAsync(_faker.System.DirectoryPath()));

            exception.Message.Should().Contain("SMB login failed with status");
            
            _mockSmbClient.Verify(c => c.Disconnect(), Times.Once);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Failure_When_TreeConnect_Fails()
        {
            // Arrange
            NTStatus treeConnectStatus = NTStatus.STATUS_BAD_NETWORK_NAME;

            _mockSmbClient
                .Setup(c => c.Connect(It.IsAny<string>(), It.IsAny<SMBTransportType>()))
                .Returns(true);

            _mockSmbClient
                .Setup(c => c.Login(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(NTStatus.STATUS_SUCCESS);

            _mockSmbClient
                .Setup(c => c.TreeConnect(It.IsAny<string>(), out treeConnectStatus))
                .Returns((ISMBFileStore)null);

            _mockSmbClient
                .Setup(c => c.IsConnected)
                .Returns(true);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.ListFilesAsync(_faker.System.DirectoryPath()));

            exception.Message.Should().Contain("Failed to connect to share");
            
            _mockSmbClient.Verify(c => c.Logoff(), Times.Once);
            _mockSmbClient.Verify(c => c.Disconnect(), Times.Once);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_RelativePath_Is_Null_And_Uses_BasePath()
        {
            // Arrange
            var mockFileName = _faker.System.FileName();
            
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory("TestBase")
                .WithFile(mockFileName);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync(null);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(f => f.FileName == mockFileName);
            
            // Verify that CreateFile was called with the base path
            _mockFileStore.Verify(
                f => f.CreateFile(
                    out It.Ref<object>.IsAny,
                    out It.Ref<FileStatus>.IsAny,
                    "TestBase",
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()),
                Times.Once);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_BasePath_Is_Null_And_Uses_RelativePath()
        {
            // Arrange
            _defaultOptions.BasePath = null;
            var mockRelativePath = "relative\\path";
            var mockFileName = _faker.System.FileName();
            
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory(mockRelativePath)
                .WithFile(mockFileName);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync(mockRelativePath);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(f => f.FileName == mockFileName);
            
            // Verify that CreateFile was called with the relative path (no base path prefix)
            _mockFileStore.Verify(
                f => f.CreateFile(
                    out It.Ref<object>.IsAny,
                    out It.Ref<FileStatus>.IsAny,
                    mockRelativePath,
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()),
                Times.Once);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_Both_Paths_Are_NonNull_And_Combines_Them()
        {
            // Arrange
            var mockRelativePath = "folder1/folder2";
            var mockFileName = _faker.System.FileName();
            var expectedCombinedPath = "TestBase\\folder1\\folder2";
            
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory(expectedCombinedPath)
                .WithFile(mockFileName);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync(mockRelativePath);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(f => f.FileName == mockFileName);
            
            // Verify that CreateFile was called with the combined path
            _mockFileStore.Verify(
                f => f.CreateFile(
                    out It.Ref<object>.IsAny,
                    out It.Ref<FileStatus>.IsAny,
                    expectedCombinedPath,
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()),
                Times.Once);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_Path_Contains_Forward_Slashes_And_Converts_To_Backslashes()
        {
            // Arrange
            var mockRelativePathWithSlashes = "folder1/folder2/folder3";
            var mockFileName = _faker.System.FileName();
            var expectedPathWithBackslashes = "TestBase\\folder1\\folder2\\folder3";
            
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory(expectedPathWithBackslashes)
                .WithFile(mockFileName);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync(mockRelativePathWithSlashes);

            // Assert
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(f => f.FileName == mockFileName);
            
            // Verify that CreateFile was called with backslashes
            _mockFileStore.Verify(
                f => f.CreateFile(
                    out It.Ref<object>.IsAny,
                    out It.Ref<FileStatus>.IsAny,
                    It.Is<string>(path => path.Contains("\\") && !path.Contains("/")),
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()),
                Times.Once);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_RoomFilter_Is_Null()
        {
            // Arrange
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory("TestBase\\2024-01-01")
                .WithSubDirectory(new MockDirectory("Room 001"))
                .WithSubDirectory(new MockDirectory("Room 002"));
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync("2024-01-01", roomFilter: null);

            // Assert
            result.Should().NotBeNull();
            
            _mockFileStore.Verify(
                f => f.QueryDirectory(
                    out It.Ref<List<QueryDirectoryFileInformation>>.IsAny,
                    It.IsAny<object>(),
                    It.IsAny<string>(),
                    It.IsAny<FileInformationClass>()),
                Times.AtLeastOnce);
        }

        [Theory]
        [InlineData("001", "Room 001", "file1.txt", true, "Room 001 should match filter 001")]
        [InlineData("001", "Room 010", "file2.txt", false, "Room 010 should not match filter 001")]
        [InlineData("1", "Room 001", "file3.txt", true, "Room 001 should match filter 1 (normalized)")]
        [InlineData("1", "Room 010", "file4.txt", false, "Room 010 should not match filter 1")]
        [InlineData("0", "Room 000", "file5.txt", true, "Room 000 should match filter 0")]
        public async Task ListFilesAsync_Returns_Success_With_Room_Filtering(
            string roomFilter,
            string roomFolderName,
            string fileName,
            bool shouldInclude,
            string reason)
        {
            // Arrange
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory("TestBase\\2024-01-01")
                .WithSubDirectory(
                    new MockDirectory(roomFolderName)
                        .WithFile(fileName));
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync("2024-01-01", roomFilter: roomFilter);

            // Assert
            if (shouldInclude)
            {
                result.Should().NotBeEmpty(reason);
                result.Should().Contain(f => f.FileName == fileName);
            }
            else
            {
                result.Should().BeEmpty(reason);
            }
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_Room_Has_Files_And_No_DateLevel_Files()
        {
            // Arrange
            var mockFileName = _faker.System.FileName();
            
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory("TestBase\\2024-01-01")
                .WithSubDirectory(
                    new MockDirectory("Room 001")
                        .WithFile(mockFileName));
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync("2024-01-01", roomFilter: "001");

            // Assert
            result.Should().NotBeEmpty();
            result.Should().OnlyContain(f => f.FileName == mockFileName);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_Room_Has_No_Files_But_DateLevel_Has_Files()
        {
            // Arrange
            var mockDateLevelFileName = _faker.System.FileName();
            
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory("TestBase\\2024-01-01")
                .WithFile(mockDateLevelFileName)
                .WithSubDirectory(new MockDirectory("Room 001"));
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync("2024-01-01", roomFilter: "001");

            // Assert
            result.Should().Contain(f => f.FileName == mockDateLevelFileName);
        }

        [Fact]
        public async Task ListFilesAsync_Returns_Success_When_Room_Has_Nested_Files()
        {
            // Arrange
            var mockFileName = _faker.System.FileName();
            var mockNestedFileName = _faker.System.FileName();
            
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory("TestBase\\2024-01-01")
                .WithSubDirectory(
                    new MockDirectory("Room 001")
                        .WithFile(mockFileName)
                        .WithSubDirectory(
                            new MockDirectory("SubFolder")
                                .WithFile(mockNestedFileName)));
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync("2024-01-01", roomFilter: "001");

            // Assert
            result.Should().HaveCountGreaterThan(0);
            result.Should().Contain(
                f => f.RelativeDirectory != null && f.RelativeDirectory.Contains("SubFolder"),
                "should include files from nested subdirectories");
        }

        [Fact]
        public async Task ListFilesAsync_Throws_IOException_When_QueryDirectory_Returns_Cancelled_Status()
        {
            // Arrange
            SetupSuccessfulConnection();
            
            var mockDir = new MockDirectory("TestBase\\2024-01-01")
                .WithSubDirectory(
                    new MockDirectory("Room 001")
                        .WithQueryDirectoryStatus(NTStatus.STATUS_CANCELLED));
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.ListFilesAsync("2024-01-01", roomFilter: "001"));

            exception.Message.Should().Contain("Failed to query directory");
            exception.Message.Should().Contain("Room 001");
        }

        [Fact]
        public async Task ListFilesAsync_Handles_RoomFilter_With_No_Digits()
        {
            // Arrange
            SetupSuccessfulConnection();

            var mockDir = new MockDirectory("TestBase\\2024-01-01")
                .WithSubDirectory(new MockDirectory("Room ABC"));
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync("2024-01-01", roomFilter: "ABC");

            // Assert
            result.Should().BeEmpty("Room filter with no digits should not match any rooms");
        }

        [Fact]
        public async Task ListFilesAsync_Handles_Empty_BasePath_And_Empty_RelativePath()
        {
            // Arrange
            _defaultOptions.BasePath = "";
            var mockFileName = _faker.System.FileName();

            SetupSuccessfulConnection();

            var mockDir = new MockDirectory("")
                .WithFile(mockFileName);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act
            var result = await client.ListFilesAsync("");

            // Assert
            result.Should().NotBeEmpty();
            result.Should().Contain(f => f.FileName == mockFileName);
        }

        #endregion

        #region Constructor and Validation Tests

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_Throws_InvalidOperationException_When_SmbServer_Is_Invalid(string serverName)
        {
            // Arrange
            _defaultOptions.SmbServer = serverName;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new SmbFileSystemClient(
                    _mockOptions.Object,
                    _mockLogger.Object,
                    _mockClientFactory.Object,
                    _noRetryPolicy));

            exception.Message.Should().Be("SmbServer is required when using SmbFileSystem");
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("  ")]
        public void Constructor_Throws_InvalidOperationException_When_SmbShareName_Is_Invalid(string shareName)
        {
            // Arrange
            _defaultOptions.SmbShareName = shareName;

            // Act & Assert
            var exception = Assert.Throws<InvalidOperationException>(() =>
                new SmbFileSystemClient(
                    _mockOptions.Object,
                    _mockLogger.Object,
                    _mockClientFactory.Object,
                    _noRetryPolicy));

            exception.Message.Should().Be("SmbShareName is required when using SmbFileSystem");
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(-1, 1)]
        [InlineData(17, 16)]
        [InlineData(20, 16)]
        [InlineData(1, 1)]
        [InlineData(16, 16)]
        public void Constructor_Adjusts_DirectoryListingMaxConcurrency_To_1_When_Below_Minimum(int initialValue, int expectedValue)
        {
            // Arrange
            _defaultOptions.DirectoryListingMaxConcurrency = initialValue;

            // Act
            var client = CreateClient();

            // Assert
            _defaultOptions.DirectoryListingMaxConcurrency.Should().Be(expectedValue);
        }

        [Fact]
        public async Task ListFilesAsync_Throws_IOException_When_Not_Found()
        {
            // Arrange
            SetupSuccessfulConnection();

            var mockDir = new MockDirectory("TestBase\\NonExistent")
                .WithCreateFileStatus(NTStatus.STATUS_NOT_FOUND);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.ListFilesAsync("NonExistent"));

            exception.Message.Should().Contain("Failed to open file/directory");
            exception.Message.Should().Contain("TestBase\\NonExistent");
        }

        [Theory]
        [InlineData(NTStatus.STATUS_OBJECT_PATH_NOT_FOUND)]
        [InlineData(NTStatus.STATUS_OBJECT_NAME_NOT_FOUND)]
        public async Task OpenFileAsync_Throws_FileNotFoundException_When_Not_Found(NTStatus status)
        {
            // Arrange
            SetupSuccessfulConnection();

            var mockDir = new MockDirectory("TestBase\\invalid\\path\\file.pdf")
                .WithCreateFileStatus(status);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<FileNotFoundException>(
                async () => await client.OpenFileAsync("invalid\\path\\file.pdf"));

            exception.Message.Should().Contain("File not found");
            exception.Message.Should().Contain("TestBase\\invalid\\path\\file.pdf");
        }

        #endregion

        #region Generic IOException Tests

        [Theory]
        [InlineData(NTStatus.STATUS_ACCESS_DENIED)]
        [InlineData(NTStatus.STATUS_SHARING_VIOLATION)]
        [InlineData(NTStatus.STATUS_DISK_FULL)]
        [InlineData(NTStatus.STATUS_INVALID_PARAMETER)]
        public async Task ListFilesAsync_Throws_IOException_For_Other_NTStatus_Errors(NTStatus errorStatus)
        {
            // Arrange
            SetupSuccessfulConnection();

            var mockDir = new MockDirectory("TestBase\\error-dir")
                .WithCreateFileStatus(errorStatus);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.ListFilesAsync("error-dir"));

            exception.Message.Should().Contain("Failed to open file/directory");
            exception.Message.Should().Contain("TestBase\\error-dir");
            exception.Message.Should().Contain(errorStatus.ToString());
        }

        [Theory]
        [InlineData(NTStatus.STATUS_ACCESS_DENIED)]
        [InlineData(NTStatus.STATUS_SHARING_VIOLATION)]
        [InlineData(NTStatus.STATUS_FILE_LOCK_CONFLICT)]
        public async Task OpenFileAsync_Throws_IOException_For_Other_NTStatus_Errors(NTStatus errorStatus)
        {
            // Arrange
            SetupSuccessfulConnection();

            var mockDir = new MockDirectory("TestBase\\error.pdf")
                .WithCreateFileStatus(errorStatus);
            SetupMockDirectoryStructure(mockDir);

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.OpenFileAsync("error.pdf"));

            exception.Message.Should().Contain("Failed to open file/directory");
            exception.Message.Should().Contain("TestBase\\error.pdf");
            exception.Message.Should().Contain(errorStatus.ToString());
        }

        #endregion

        #region OpenFileAsync Tests

        [Fact]
        public async Task OpenFileAsync_Handles_Null_Data_From_ReadFile()
        {
            // Arrange
            SetupSuccessfulConnection();
            
            var mockPath = "TestBase\\test.pdf";
            var fileHandle = new object();

            _mockFileStore
                .Setup(f => f.CreateFile(
                    out fileHandle,
                    out It.Ref<FileStatus>.IsAny,
                    mockPath,
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()))
                .Returns(NTStatus.STATUS_SUCCESS);

            _mockFileStore
                .Setup(f => f.ReadFile(out It.Ref<byte[]>.IsAny, fileHandle, It.IsAny<long>(), It.IsAny<int>()))
                .Returns((out byte[] data, object handle, long offset, int maxCount) =>
                {
                    data = null!;
                    return NTStatus.STATUS_SUCCESS;
                });

            _mockFileStore
                .Setup(f => f.CloseFile(fileHandle));

            var client = CreateClient();

            // Act
            var result = await client.OpenFileAsync("test.pdf");

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(0);
            
            _mockFileStore.Verify(f => f.CloseFile(fileHandle), Times.Once);
        }

        [Fact]
        public async Task OpenFileAsync_Handles_Empty_Data_From_ReadFile()
        {
            // Arrange
            SetupSuccessfulConnection();
            
            var mockPath = "TestBase\\test.pdf";
            var fileHandle = new object();

            _mockFileStore
                .Setup(f => f.CreateFile(
                    out fileHandle,
                    out It.Ref<FileStatus>.IsAny,
                    mockPath,
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()))
                .Returns(NTStatus.STATUS_SUCCESS);

            _mockFileStore
                .Setup(f => f.ReadFile(out It.Ref<byte[]>.IsAny, fileHandle, It.IsAny<long>(), It.IsAny<int>()))
                .Returns((out byte[] data, object handle, long offset, int maxCount) =>
                {
                    data = Array.Empty<byte>();
                    return NTStatus.STATUS_SUCCESS;
                });

            _mockFileStore
                .Setup(f => f.CloseFile(fileHandle));

            var client = CreateClient();

            // Act
            var result = await client.OpenFileAsync("test.pdf");

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(0);
            
            _mockFileStore.Verify(f => f.CloseFile(fileHandle), Times.Once);
        }

        [Fact]
        public async Task OpenFileAsync_Returns_Complete_File_When_Data_Arrives_In_Multiple_Chunks()
        {
            // Arrange
            SetupSuccessfulConnection();
            
            var mockPath = "TestBase\\test.pdf";
            var fileHandle = new object();
            var chunk1 = new byte[] { 1, 2, 3, 4, 5 };
            var chunk2 = new byte[] { 6, 7, 8, 9, 10 };
            var callCount = 0;

            _mockFileStore
                .Setup(f => f.CreateFile(
                    out fileHandle,
                    out It.Ref<FileStatus>.IsAny,
                    mockPath,
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()))
                .Returns(NTStatus.STATUS_SUCCESS);

            _mockFileStore
                .Setup(f => f.ReadFile(out It.Ref<byte[]>.IsAny, fileHandle, It.IsAny<long>(), It.IsAny<int>()))
                .Returns((out byte[] data, object handle, long offset, int maxCount) =>
                {
                    callCount++;
                    if (callCount == 1)
                    {
                        data = chunk1;
                        return NTStatus.STATUS_SUCCESS;
                    }
                    else if (callCount == 2)
                    {
                        data = chunk2;
                        return NTStatus.STATUS_END_OF_FILE;
                    }
                    else
                    {
                        data = Array.Empty<byte>();
                        return NTStatus.STATUS_END_OF_FILE;
                    }
                });

            _mockFileStore
                .Setup(f => f.CloseFile(fileHandle));

            var client = CreateClient();

            // Act
            var result = await client.OpenFileAsync("test.pdf");

            // Assert
            result.Should().NotBeNull();
            result.Length.Should().Be(10);
            
            var buffer = new byte[10];
            await result.ReadExactlyAsync(buffer, 0, 10);
            buffer.Should().BeEquivalentTo(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            
            _mockFileStore.Verify(f => f.CloseFile(fileHandle), Times.Once);
        }

        [Fact]
        public async Task OpenFileAsync_Throws_IOException_When_ReadFile_Returns_Error_Status()
        {
            // Arrange
            SetupSuccessfulConnection();
            
            var mockPath = "TestBase\\test.pdf";
            var fileHandle = new object();

            _mockFileStore
                .Setup(f => f.CreateFile(
                    out fileHandle,
                    out It.Ref<FileStatus>.IsAny,
                    mockPath,
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()))
                .Returns(NTStatus.STATUS_SUCCESS);

            _mockFileStore
                .Setup(f => f.ReadFile(out It.Ref<byte[]>.IsAny, fileHandle, It.IsAny<long>(), It.IsAny<int>()))
                .Returns((out byte[] data, object handle, long offset, int maxCount) =>
                {
                    data = new byte[] { 1, 2, 3 };
                    return NTStatus.STATUS_ACCESS_DENIED;
                });

            _mockFileStore
                .Setup(f => f.CloseFile(fileHandle));

            var client = CreateClient();

            // Act & Assert
            var exception = await Assert.ThrowsAsync<IOException>(
                async () => await client.OpenFileAsync("test.pdf"));

            exception.Message.Should().Contain("Failed to read file");
            exception.Message.Should().Contain("test.pdf");
            
            _mockFileStore.Verify(f => f.CloseFile(fileHandle), Times.Once);
        }

        [Fact]
        public async Task CreateConnectionAsync_Disconnects_Client_When_Exception_Occurs()
        {
            // Arrange
            _mockSmbClient
                .Setup(c => c.Connect(It.IsAny<string>(), It.IsAny<SMBTransportType>()))
                .Returns(true);

            _mockSmbClient
                .Setup(c => c.Login(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Test exception"));

            _mockSmbClient
                .Setup(c => c.IsConnected)
                .Returns(true);

            var client = CreateClient();

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(
                async () => await client.ListFilesAsync("test-path"));

            // Verify disconnect was called in finally block
            _mockSmbClient.Verify(c => c.Disconnect(), Times.AtLeastOnce);
        }

        #endregion

        #region Helper Methods

        private SmbFileSystemClient CreateClient()
        {
            return new SmbFileSystemClient(
                _mockOptions.Object,
                _mockLogger.Object,
                _mockClientFactory.Object,
                _noRetryPolicy);
        }

        private void SetupSuccessfulConnection()
        {
            NTStatus successStatus = NTStatus.STATUS_SUCCESS;

            _mockSmbClient
                .Setup(c => c.Connect(It.IsAny<string>(), It.IsAny<SMBTransportType>()))
                .Returns(true);

            _mockSmbClient
                .Setup(c => c.Login(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(NTStatus.STATUS_SUCCESS);

            _mockSmbClient
                .Setup(c => c.TreeConnect(It.IsAny<string>(), out successStatus))
                .Returns(_mockFileStore.Object);

            _mockSmbClient
                .Setup(c => c.IsConnected)
                .Returns(true);
        }

        /// <summary>
        /// Recursively creates mock directory structure in the MockFileStore based on nested MockDirectory objects.
        /// </summary>
        /// <param name="directory">The mock directory to setup</param>
        /// <param name="basePath">Base path to prepend to directory names (empty for root)</param>
        private void SetupMockDirectoryStructure(MockDirectory directory, string basePath = "")
        {
            // Construct the full path for this directory
            var currentPath = string.IsNullOrEmpty(basePath)
                ? directory.Name
                : $"{basePath}\\{directory.Name}";

            // Create a unique handle for this directory
            var directoryHandle = new object();

            // Build the list of items (files and subdirectories) to be returned by QueryDirectory
            var directoryContents = new List<QueryDirectoryFileInformation>();

            // Add files to the directory contents
            foreach (var file in directory.Files)
            {
                directoryContents.Add(CreateFileInfo(file.Name));
            }

            // Add subdirectories to the directory contents
            foreach (var subDir in directory.SubDirectories)
            {
                directoryContents.Add(CreateDirectoryInfo(subDir.Name));
            }

            // Setup CreateFile mock for this directory
            _mockFileStore
                .Setup(f => f.CreateFile(
                    out directoryHandle,
                    out It.Ref<FileStatus>.IsAny,
                    currentPath,
                    It.IsAny<AccessMask>(),
                    It.IsAny<SMBLibrary.FileAttributes>(),
                    It.IsAny<ShareAccess>(),
                    It.IsAny<CreateDisposition>(),
                    It.IsAny<CreateOptions>(),
                    It.IsAny<SecurityContext>()))
                .Returns(directory.CreateFileStatus);

            // Setup QueryDirectory mock for this directory (only if CreateFile succeeds)
            if (directory.CreateFileStatus == NTStatus.STATUS_SUCCESS)
            {
                _mockFileStore
                    .Setup(f => f.QueryDirectory(
                        out directoryContents,
                        directoryHandle,
                        It.IsAny<string>(),
                        It.IsAny<FileInformationClass>()))
                    .Returns(directory.QueryDirectoryStatus);

                // Setup CloseFile mock for this directory handle
                _mockFileStore
                    .Setup(f => f.CloseFile(directoryHandle));
            }

            // Recursively setup all subdirectories
            foreach (var subDir in directory.SubDirectories)
            {
                SetupMockDirectoryStructure(subDir, currentPath);
            }
        }

        private FileDirectoryInformation CreateFileInfo(string fileName)
        {
            return new FileDirectoryInformation
            {
                FileName = fileName,
                FileAttributes = SMBLibrary.FileAttributes.Normal,
                EndOfFile = _faker.Random.Long(1024, 1048576),
                CreationTime = _faker.Date.Past()
            };
        }

        private FileDirectoryInformation CreateDirectoryInfo(string directoryName)
        {
            return new FileDirectoryInformation
            {
                FileName = directoryName,
                FileAttributes = SMBLibrary.FileAttributes.Directory,
                EndOfFile = 0,
                CreationTime = _faker.Date.Past()
            };
        }

        #endregion
    }

    #region Mock Directory Models

    /// <summary>
    /// Represents a mock file for testing purposes.
    /// </summary>
    public class MockFile
    {
        public string Name { get; set; }

        public MockFile(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Represents a mock directory structure for testing purposes.
    /// </summary>
    public class MockDirectory
    {
        public string Name { get; set; }
        public List<MockFile> Files { get; set; } = new();
        public List<MockDirectory> SubDirectories { get; set; } = new();
        public NTStatus CreateFileStatus { get; set; } = NTStatus.STATUS_SUCCESS;
        public NTStatus QueryDirectoryStatus { get; set; } = NTStatus.STATUS_SUCCESS;

        public MockDirectory(string name)
        {
            Name = name;
        }

        public MockDirectory WithFile(string fileName)
        {
            Files.Add(new MockFile(fileName));
            return this;
        }

        public MockDirectory WithFiles(params string[] fileNames)
        {
            foreach (var fileName in fileNames)
            {
                Files.Add(new MockFile(fileName));
            }
            return this;
        }

        public MockDirectory WithSubDirectory(MockDirectory subDirectory)
        {
            SubDirectories.Add(subDirectory);
            return this;
        }

        public MockDirectory WithCreateFileStatus(NTStatus status)
        {
            CreateFileStatus = status;
            return this;
        }

        public MockDirectory WithQueryDirectoryStatus(NTStatus status)
        {
            QueryDirectoryStatus = status;
            return this;
        }
    }

    #endregion
}
