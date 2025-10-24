using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Scv.TdApi.Infrastructure.Options;

namespace Scv.TdApi.Infrastructure.FileSystem
{
    /// <summary>
    /// Local file system implementation of ISmbFileSystemClient.
    /// Uses standard System.IO for file operations.
    /// </summary>
    public sealed class LocalFileSystemClient : ISmbFileSystemClient
    {
        private readonly string _basePath;
        private readonly ILogger<LocalFileSystemClient> _logger;
        private bool _disposed;

        public LocalFileSystemClient(
            IOptions<SharedDriveOptions> options,
            ILogger<LocalFileSystemClient> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            var opts = options?.Value ?? throw new ArgumentNullException(nameof(options));
            
            _basePath = Path.GetFullPath(opts.BasePath);
            
            if (!Directory.Exists(_basePath))
            {
                throw new DirectoryNotFoundException($"Base path does not exist: {_basePath}");
            }

            _logger.LogInformation("LocalFileSystemClient initialized with base path: {BasePath}", _basePath);
        }

        public Task<IReadOnlyList<SmbFileInfo>> ListFilesAsync(
            string path, 
            string? roomFilter = null, 
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            var fullPath = GetFullPath(path);
            _logger.LogDebug("Listing files in directory: {Path} with room filter: {RoomFilter}", fullPath, roomFilter ?? "none");

            if (!Directory.Exists(fullPath))
            {
                _logger.LogDebug("Directory does not exist: {Path}", fullPath);
                return Task.FromResult<IReadOnlyList<SmbFileInfo>>(Array.Empty<SmbFileInfo>());
            }

            try
            {
                var files = new List<SmbFileInfo>();
                var matchingRoomDirectories = new List<string>();

                // Single enumeration to get both files and directories
                foreach (var entry in Directory.EnumerateFileSystemEntries(fullPath, "*", SearchOption.TopDirectoryOnly))
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (File.Exists(entry))
                    {
                        // It's a file in the top-level directory
                        if (TryCreateSmbFileInfo(entry, fullPath, out var fileInfo))
                        {
                            files.Add(fileInfo);
                        }
                    }
                    else if (Directory.Exists(entry))
                    {
                        // It's a subdirectory - check if it matches the room filter
                        if (!string.IsNullOrWhiteSpace(roomFilter))
                        {
                            var dirName = Path.GetFileName(entry);
                            if (dirName.Contains(roomFilter, StringComparison.OrdinalIgnoreCase))
                            {
                                matchingRoomDirectories.Add(entry);
                                _logger.LogDebug("Found matching room directory: {Directory}", entry);
                            }
                        }
                    }
                }

                // List files in matching room subdirectories
                foreach (var roomDirectory in matchingRoomDirectories)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    
                    foreach (var filePath in Directory.EnumerateFiles(roomDirectory, "*", SearchOption.TopDirectoryOnly))
                    {
                        if (TryCreateSmbFileInfo(filePath, fullPath, out var fileInfo))
                        {
                            files.Add(fileInfo);
                        }
                    }
                }

                _logger.LogDebug("Found {Count} files in {Path} ({RoomCount} room directories)", 
                    files.Count, fullPath, matchingRoomDirectories.Count);
                
                return Task.FromResult<IReadOnlyList<SmbFileInfo>>(files);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to list files in directory: {Path}", fullPath);
                throw new IOException($"Failed to list files in directory: {fullPath}", ex);
            }
        }

        public Task<Stream> OpenFileAsync(string filePath, CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();
            
            var fullPath = GetFullPath(filePath);
            _logger.LogDebug("Opening file: {Path}", fullPath);

            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"File not found: {filePath}", fullPath);
            }

            try
            {
                var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 
                    bufferSize: 81920, useAsync: true);
                
                _logger.LogDebug("Successfully opened file: {Path}", fullPath);
                return Task.FromResult<Stream>(fileStream);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open file: {Path}", fullPath);
                throw new IOException($"Failed to open file: {filePath}", ex);
            }
        }

        private string GetFullPath(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
            {
                return _basePath;
            }

            var combined = Path.Combine(_basePath, relativePath.TrimStart('/', '\\'));
            var fullPath = Path.GetFullPath(combined);

            // Security check: ensure path is under base path
            if (!fullPath.StartsWith(_basePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException($"Path is outside base directory: {relativePath}");
            }

            return fullPath;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(LocalFileSystemClient));
            }
        }

        private bool TryCreateSmbFileInfo(string filePath, string rootPath, out SmbFileInfo fileInfo)
        {
            try
            {
                var fi = new FileInfo(filePath);
                var relativePath = Path.GetRelativePath(rootPath, filePath);
                var relativeDir = Path.GetDirectoryName(relativePath);

                fileInfo = new SmbFileInfo
                {
                    FileName = fi.Name,
                    FullPath = fi.FullName,
                    Extension = fi.Extension,
                    SizeBytes = fi.Length,
                    CreatedUtc = fi.CreationTimeUtc,
                    RelativeDirectory = string.IsNullOrEmpty(relativeDir) ? null : relativeDir
                };
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get file info for: {FilePath}", filePath);
                fileInfo = default!;
                return false;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogDebug("Disposing LocalFileSystemClient");
                _disposed = true;
            }
        }
    }
}