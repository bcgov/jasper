using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Scv.TdApi.Infrastructure.Options;
using SMBLibrary;
using SMBLibrary.Client;

namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// SMB file system implementation using SMBLibrary.
    /// Thread-safe with proper connection lifecycle management.
    /// Each operation gets its own isolated connection to prevent cross-operation interference.
    /// Supports parallel iterative traversal of room subdirectories for optimal performance.
    /// </summary>
    public sealed class SmbFileSystemClient : ISmbFileSystemClient
    {
        private readonly SharedDriveOptions _options;
        private readonly ILogger<SmbFileSystemClient> _logger;
        private readonly AsyncRetryPolicy _retryPolicy;
        private bool _disposed;

        public SmbFileSystemClient(
            IOptions<SharedDriveOptions> options,
            ILogger<SmbFileSystemClient> logger)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            ValidateOptions();

            _retryPolicy = CreateRetryPolicy();

            _logger.LogInformation(
                "SmbFileSystemClient initialized for server: {Server}, share: {Share}, max concurrency: {MaxConcurrency}",
                _options.SmbServer,
                _options.SmbShareName,
                _options.DirectoryListingMaxConcurrency);
        }

        private void ValidateOptions()
        {
            if (string.IsNullOrWhiteSpace(_options.SmbServer))
            {
                throw new InvalidOperationException("SmbServer is required when using SmbFileSystem");
            }

            if (string.IsNullOrWhiteSpace(_options.SmbShareName))
            {
                throw new InvalidOperationException("SmbShareName is required when using SmbFileSystem");
            }

            if (_options.DirectoryListingMaxConcurrency < 1)
            {
                _logger.LogWarning("DirectoryListingMaxConcurrency is {Value}, setting to 1", _options.DirectoryListingMaxConcurrency);
                _options.DirectoryListingMaxConcurrency = 1;
            }
            else if (_options.DirectoryListingMaxConcurrency > 16)
            {
                _logger.LogWarning("DirectoryListingMaxConcurrency is {Value}, capping at 16", _options.DirectoryListingMaxConcurrency);
                _options.DirectoryListingMaxConcurrency = 16;
            }
        }

        private AsyncRetryPolicy CreateRetryPolicy()
        {
            return Policy
                .Handle<IOException>()
                .Or<Exception>(ex => ex.Message.Contains("connection", StringComparison.OrdinalIgnoreCase))
                .WaitAndRetryAsync(
                    retryCount: _options.MaxRetryAttempts,
                    sleepDurationProvider: retryAttempt =>
                        TimeSpan.FromMilliseconds(_options.InitialRetryDelayMs * Math.Pow(2, retryAttempt - 1)),
                    onRetry: (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            exception,
                            "SMB operation failed. Retry {RetryCount}/{MaxRetries} after {Delay}ms",
                            retryCount,
                            _options.MaxRetryAttempts,
                            timeSpan.TotalMilliseconds);
                    });
        }

        /// <summary>
        /// Normalizes room code by extracting the numeric portion and removing leading zeros.
        /// Examples: "009" -> "9", "Courtroom 009" -> "9", "R009" -> "9"
        /// </summary>
        private string? NormalizeRoomCode(string? roomCode)
        {
            if (string.IsNullOrWhiteSpace(roomCode))
                return roomCode;

            var match = Regex.Match(roomCode, @"\d+");
            if (match.Success)
            {
                var normalized = match.Value.TrimStart('0');
                var result = string.IsNullOrEmpty(normalized) ? "0" : normalized;
                
                _logger.LogDebug("Normalized room code from '{Original}' to '{Normalized}'", roomCode, result);
                return result;
            }

            return roomCode;
        }

        /// <summary>
        /// Checks if a folder name matches the normalized room code.
        /// Uses numeric matching to ensure "9" matches "Room 9" or "R 9" but not "Room 69".
        /// </summary>
        private bool IsRoomMatch(string folderName, string normalizedRoomCode)
        {
            if (string.IsNullOrWhiteSpace(folderName) || string.IsNullOrWhiteSpace(normalizedRoomCode))
                return false;

            var matches = Regex.Matches(folderName, @"\d+");
            
            foreach (Match match in matches)
            {
                var normalizedFolderNumber = match.Value.TrimStart('0');
                if (string.IsNullOrEmpty(normalizedFolderNumber))
                    normalizedFolderNumber = "0";

                if (normalizedFolderNumber.Equals(normalizedRoomCode, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogDebug("Room match found: folder '{Folder}' contains number '{Number}' matching '{Target}'", 
                        folderName, match.Value, normalizedRoomCode);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Creates a new isolated SMB connection for a single operation.
        /// This prevents connection sharing issues between concurrent operations.
        /// </summary>
        private async Task<SmbConnection> CreateConnectionAsync(CancellationToken cancellationToken)
        {
            ThrowIfDisposed();

            return await Task.Run(() =>
            {
                _logger.LogDebug("Establishing new SMB connection to {Server}\\{Share}", _options.SmbServer, _options.SmbShareName);

                var client = new SMB2Client();

                try
                {
                    var connected = client.Connect(_options.SmbServer!, SMBTransportType.DirectTCPTransport);
                    if (!connected)
                    {
                        throw new IOException($"Failed to connect to SMB server: {_options.SmbServer}");
                    }

                    var loginStatus = client.Login(
                        _options.SmbDomain ?? string.Empty,
                        _options.SmbUsername ?? string.Empty,
                        _options.SmbPassword ?? string.Empty);

                    if (loginStatus != NTStatus.STATUS_SUCCESS)
                    {
                        client.Disconnect();
                        throw new IOException($"SMB login failed with status: {loginStatus}");
                    }

                    var fileStore = client.TreeConnect(_options.SmbShareName!, out var treeConnectStatus);
                    if (treeConnectStatus != NTStatus.STATUS_SUCCESS)
                    {
                        client.Logoff();
                        client.Disconnect();
                        throw new IOException($"Failed to connect to share '{_options.SmbShareName}' with status: {treeConnectStatus}");
                    }

                    _logger.LogDebug("Successfully established SMB connection");
                    return new SmbConnection(client, fileStore, _logger);
                }
                catch
                {
                    client.Disconnect();
                    throw;
                }
            }, cancellationToken);
        }

        public async Task<IReadOnlyList<SmbFileInfo>> ListFilesAsync(
            string path, 
            string? roomFilter = null, 
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return await _retryPolicy.ExecuteAsync(async ct =>
            {
                using var connection = await CreateConnectionAsync(ct);

                var smbPath = GetSmbPath(path);
                var normalizedRoomFilter = NormalizeRoomCode(roomFilter);
                
                _logger.LogDebug("Listing files in SMB path: {Path} with normalized room filter: {RoomFilter}", 
                    smbPath, normalizedRoomFilter ?? "none");

                var files = new List<SmbFileInfo>();
                
                // List files with normalized room matching
                await ListFilesWithRoomFilterAsync(connection.FileStore, smbPath, smbPath, normalizedRoomFilter, files, ct);

                _logger.LogDebug("Found {Count} files in {Path}", files.Count, smbPath);
                return (IReadOnlyList<SmbFileInfo>)files;
            }, cancellationToken);
        }

        /// <summary>
        /// Lists files in a directory, and if a room filter is provided, also lists files in matching subdirectories.
        /// Uses parallel iterative traversal for room subdirectories with bounded concurrency.
        /// Makes 1 SMB call to get directory contents, then parallel calls for room subdirectory trees.
        /// </summary>
        private async Task ListFilesWithRoomFilterAsync(
            ISMBFileStore fileStore,
            string directoryPath,
            string rootPath,
            string? normalizedRoomFilter,
            List<SmbFileInfo> files,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // SMB Call #1: Open and query the top-level directory (gets both files AND subdirectories)
            var status = fileStore.CreateFile(
                out var directoryHandle,
                out _,
                directoryPath,
                AccessMask.GENERIC_READ,
                SMBLibrary.FileAttributes.Directory,
                ShareAccess.Read,
                CreateDisposition.FILE_OPEN,
                CreateOptions.FILE_DIRECTORY_FILE,
                null);

            if (status != NTStatus.STATUS_SUCCESS)
            {
                if (status == NTStatus.STATUS_OBJECT_NAME_NOT_FOUND ||
                    status == NTStatus.STATUS_OBJECT_PATH_NOT_FOUND)
                {
                    _logger.LogDebug("Directory not found: {Path}", directoryPath);
                    return;
                }

                throw new IOException($"Failed to open directory '{directoryPath}': {status}");
            }

            var matchingRoomDirectories = new List<string>();

            try
            {
                status = fileStore.QueryDirectory(
                    out var fileList,
                    directoryHandle,
                    "*",
                    FileInformationClass.FileDirectoryInformation);

                if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_NO_MORE_FILES)
                {
                    throw new IOException($"Failed to query directory '{directoryPath}': {status}");
                }

                // Single pass: collect top-level files and matching room directories
                foreach (var item in fileList ?? Enumerable.Empty<QueryDirectoryFileInformation>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (item is not FileDirectoryInformation fileInfo)
                        continue;

                    if (fileInfo.FileName == "." || fileInfo.FileName == "..")
                        continue;

                    var itemPath = CombineSmbPath(directoryPath, fileInfo.FileName);
                    var isDirectory = (fileInfo.FileAttributes & SMBLibrary.FileAttributes.Directory) != 0;

                    if (isDirectory)
                    {
                        // If no room filter is specified, include all room directories
                        // If room filter is specified, only include matching directories
                        if (string.IsNullOrWhiteSpace(normalizedRoomFilter) || 
                            IsRoomMatch(fileInfo.FileName, normalizedRoomFilter))
                        {
                            matchingRoomDirectories.Add(itemPath);
                            _logger.LogDebug("Found matching room directory: {Path}", itemPath);
                        }
                        else
                        {
                            _logger.LogDebug("Skipping non-matching room directory: {Path}", fileInfo.FileName);
                        }
                    }
                    else
                    {
                        // Add top-level files (always included regardless of room filter)
                        var relativePath = itemPath.Substring(rootPath.Length).TrimStart('\\');
                        var relativeDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');

                        files.Add(new SmbFileInfo
                        {
                            FileName = fileInfo.FileName,
                            FullPath = itemPath,
                            Extension = Path.GetExtension(fileInfo.FileName),
                            SizeBytes = fileInfo.EndOfFile,
                            CreatedUtc = fileInfo.CreationTime.ToUniversalTime(),
                            RelativeDirectory = string.IsNullOrEmpty(relativeDir) ? null : relativeDir
                        });
                    }
                }
            }
            finally
            {
                fileStore.CloseFile(directoryHandle);
            }

            if (matchingRoomDirectories.Count == 0)
            {
                _logger.LogDebug("No matching room directories found");
                return;
            }

            // Parallel iterative traversal of room subdirectories
            var roomFiles = await TraverseRoomDirectoriesParallelAsync(
                fileStore,
                matchingRoomDirectories,
                rootPath,
                cancellationToken);

            files.AddRange(roomFiles);

            _logger.LogDebug(
                "Completed traversal: {TopLevelFiles} top-level files, {RoomFiles} room files, {RoomDirs} room directories",
                files.Count - roomFiles.Count,
                roomFiles.Count,
                matchingRoomDirectories.Count);
        }

        /// <summary>
        /// Traverses multiple room directory trees in parallel using bounded concurrency.
        /// Uses iterative (stack-based) traversal to avoid deep recursion.
        /// Thread-safe: uses concurrent collections and synchronization primitives.
        /// </summary>
        private async Task<List<SmbFileInfo>> TraverseRoomDirectoriesParallelAsync(
            ISMBFileStore fileStore,
            List<string> roomDirectories,
            string rootPath,
            CancellationToken cancellationToken)
        {
            var allFiles = new ConcurrentBag<SmbFileInfo>();
            var maxConcurrency = _options.DirectoryListingMaxConcurrency;

            _logger.LogDebug(
                "Starting parallel traversal of {RoomCount} room directories with max concurrency {MaxConcurrency}",
                roomDirectories.Count,
                maxConcurrency);

            // Use SemaphoreSlim to limit concurrency
            using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

            var tasks = roomDirectories.Select(async roomDir =>
            {
                await semaphore.WaitAsync(cancellationToken);
                try
                {
                    var files = await Task.Run(() => 
                        TraverseDirectoryTreeIterative(fileStore, roomDir, rootPath, cancellationToken), 
                        cancellationToken);
                    
                    foreach (var file in files)
                    {
                        allFiles.Add(file);
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            }).ToList();

            await Task.WhenAll(tasks);

            return allFiles.ToList();
        }

        /// <summary>
        /// Iteratively traverses a directory tree using a stack (avoids recursion).
        /// Returns all files found in the tree.
        /// </summary>
        private List<SmbFileInfo> TraverseDirectoryTreeIterative(
            ISMBFileStore fileStore,
            string startDirectory,
            string rootPath,
            CancellationToken cancellationToken)
        {
            var files = new List<SmbFileInfo>();
            var directoryStack = new Stack<string>();
            directoryStack.Push(startDirectory);

            while (directoryStack.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var currentDir = directoryStack.Pop();

                try
                {
                    var status = fileStore.CreateFile(
                        out var dirHandle,
                        out _,
                        currentDir,
                        AccessMask.GENERIC_READ,
                        SMBLibrary.FileAttributes.Directory,
                        ShareAccess.Read,
                        CreateDisposition.FILE_OPEN,
                        CreateOptions.FILE_DIRECTORY_FILE,
                        null);

                    if (status != NTStatus.STATUS_SUCCESS)
                    {
                        if (status == NTStatus.STATUS_OBJECT_NAME_NOT_FOUND ||
                            status == NTStatus.STATUS_OBJECT_PATH_NOT_FOUND)
                        {
                            _logger.LogDebug("Directory not found during traversal: {Path}", currentDir);
                            continue;
                        }

                        throw new IOException($"Failed to open directory '{currentDir}': {status}");
                    }

                    try
                    {
                        status = fileStore.QueryDirectory(
                            out var fileList,
                            dirHandle,
                            "*",
                            FileInformationClass.FileDirectoryInformation);

                        if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_NO_MORE_FILES)
                        {
                            throw new IOException($"Failed to query directory '{currentDir}': {status}");
                        }

                        foreach (var item in fileList ?? Enumerable.Empty<QueryDirectoryFileInformation>())
                        {
                            cancellationToken.ThrowIfCancellationRequested();

                            if (item is not FileDirectoryInformation fileInfo)
                                continue;

                            if (fileInfo.FileName == "." || fileInfo.FileName == "..")
                                continue;

                            var itemPath = CombineSmbPath(currentDir, fileInfo.FileName);
                            var isDirectory = (fileInfo.FileAttributes & SMBLibrary.FileAttributes.Directory) != 0;

                            if (isDirectory)
                            {
                                // Push subdirectory onto stack for later processing
                                directoryStack.Push(itemPath);
                            }
                            else
                            {
                                // Add file to results
                                var relativePath = itemPath.Substring(rootPath.Length).TrimStart('\\');
                                var relativeDir = Path.GetDirectoryName(relativePath)?.Replace('\\', '/');

                                files.Add(new SmbFileInfo
                                {
                                    FileName = fileInfo.FileName,
                                    FullPath = itemPath,
                                    Extension = Path.GetExtension(fileInfo.FileName),
                                    SizeBytes = fileInfo.EndOfFile,
                                    CreatedUtc = fileInfo.CreationTime.ToUniversalTime(),
                                    RelativeDirectory = string.IsNullOrEmpty(relativeDir) ? null : relativeDir
                                });
                            }
                        }
                    }
                    finally
                    {
                        fileStore.CloseFile(dirHandle);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error processing directory {Dir}, skipping", currentDir);
                    // Continue processing other directories
                }
            }

            _logger.LogDebug("Traversed directory tree {StartDir}, found {FileCount} files", startDirectory, files.Count);
            return files;
        }

        public async Task<Stream> OpenFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            return await _retryPolicy.ExecuteAsync(async ct =>
            {
                using var connection = await CreateConnectionAsync(ct);

                var smbPath = GetSmbPath(filePath);
                _logger.LogDebug("Opening SMB file: {Path}", smbPath);

                var status = connection.FileStore.CreateFile(
                    out var fileHandle,
                    out _,
                    smbPath,
                    AccessMask.GENERIC_READ | AccessMask.SYNCHRONIZE,
                    SMBLibrary.FileAttributes.Normal,
                    ShareAccess.Read,
                    CreateDisposition.FILE_OPEN,
                    CreateOptions.FILE_NON_DIRECTORY_FILE | CreateOptions.FILE_SYNCHRONOUS_IO_ALERT,
                    null);

                if (status != NTStatus.STATUS_SUCCESS)
                {
                    if (status == NTStatus.STATUS_OBJECT_NAME_NOT_FOUND)
                    {
                        throw new FileNotFoundException($"File not found: {filePath}");
                    }

                    throw new IOException($"Failed to open file '{filePath}': {status}");
                }

                try
                {
                    var memoryStream = new MemoryStream();
                    long offset = 0;
                    const int bufferSize = 64 * 1024; // 64KB buffer

                    while (true)
                    {
                        ct.ThrowIfCancellationRequested();

                        status = connection.FileStore.ReadFile(out var data, fileHandle, offset, bufferSize); // read whole file to avoid smb connection causing premature file stream disposal. Could move to a connection pool to bypass this.

                        if (status != NTStatus.STATUS_SUCCESS && status != NTStatus.STATUS_END_OF_FILE)
                        {
                            throw new IOException($"Failed to read file '{filePath}': {status}");
                        }

                        if (data == null || data.Length == 0)
                        {
                            break;
                        }

                        await memoryStream.WriteAsync(data, ct);
                        offset += data.Length;

                        if (status == NTStatus.STATUS_END_OF_FILE)
                        {
                            break;
                        }
                    }

                    memoryStream.Position = 0;
                    _logger.LogDebug("Successfully read {Bytes} bytes from {Path}", memoryStream.Length, smbPath);
                    return (Stream)memoryStream;
                }
                finally
                {
                    connection.FileStore.CloseFile(fileHandle);
                }
            }, cancellationToken);
        }

        private string GetSmbPath(string relativePath)
        {
            var basePath = _options.BasePath?.Trim('/', '\\') ?? string.Empty;
            var relPath = relativePath?.Trim('/', '\\') ?? string.Empty;

            var combined = string.IsNullOrEmpty(relPath)
                ? basePath
                : string.IsNullOrEmpty(basePath)
                    ? relPath
                    : $"{basePath}\\{relPath}";

            return combined.Replace('/', '\\');
        }

        private static string CombineSmbPath(string path1, string path2)
        {
            return $"{path1.TrimEnd('\\')}\\{path2.TrimStart('\\')}";
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(SmbFileSystemClient));
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _logger.LogDebug("Disposing SmbFileSystemClient");
            _disposed = true;
        }

        /// <summary>
        /// Represents an isolated SMB connection that can be safely disposed
        /// without affecting other operations.
        /// </summary>
        private sealed class SmbConnection : IDisposable
        {
            private readonly SMB2Client _client;
            private readonly ILogger _logger;
            private bool _disposed;

            public ISMBFileStore FileStore { get; }

            public SmbConnection(SMB2Client client, ISMBFileStore fileStore, ILogger logger)
            {
                _client = client ?? throw new ArgumentNullException(nameof(client));
                FileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                try
                {
                    FileStore?.Disconnect();
                    _client?.Logoff();
                    _client?.Disconnect();
                    _logger.LogDebug("SMB connection disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error while disposing SMB connection");
                }
                finally
                {
                    _disposed = true;
                }
            }
        }
    }
}