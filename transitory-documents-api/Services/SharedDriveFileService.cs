using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Scv.Models;
using Scv.TdApi.Infrastructure.FileSystem;
using Scv.TdApi.Infrastructure.Options;
using Scv.TdApi.Models;
using System.Collections.Concurrent;

namespace Scv.TdApi.Services
{
    public sealed class SharedDriveFileService : ISharedDriveFileService
    {
        private readonly ISmbFileSystemClient _fileSystemClient;
        private readonly ILogger<SharedDriveFileService> _logger;
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
        private readonly SharedDriveOptions _options;
        private readonly CorrectionMappingOptions _correctionMappingOptions;

        public SharedDriveFileService(
            ISmbFileSystemClient fileSystemClient,
            ILogger<SharedDriveFileService> logger,
            IOptions<SharedDriveOptions> options,
            IOptions<CorrectionMappingOptions> correctionMappingOptions)
        {
            _fileSystemClient = fileSystemClient ?? throw new ArgumentNullException(nameof(fileSystemClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            _correctionMappingOptions = correctionMappingOptions?.Value ?? throw new ArgumentNullException(nameof(correctionMappingOptions));
        }

        public IReadOnlyList<FileMetadataDto> FindFilesAsync(
            string region,
            string location,
            string roomCd,
            DateOnly date)
        {
            try
            {
                _logger.LogInformation(
                    "Finding files for region: {Region}, location: {Location}, room: {Room}, date: {Date}",
                    region, location, roomCd, date);

                region = _correctionMappingOptions.RegionMappings
                    .FirstOrDefault(m => string.Equals(m.Target, region, StringComparison.OrdinalIgnoreCase))
                    ?.Replacement ?? region;

                location = _correctionMappingOptions.LocationMappings
                    .FirstOrDefault(m => string.Equals(m.Target, location, StringComparison.OrdinalIgnoreCase))
                    ?.Replacement ?? location;

                var locationPath = Path.Combine(region, location).Replace('\\', '/');
                var candidateDatePaths = GetCandidateDatePaths(locationPath, date);
                var allFiles = new ConcurrentDictionary<string, FileMetadataDto>(StringComparer.OrdinalIgnoreCase);

                Parallel.ForEach(candidateDatePaths, datePath =>
                {
                    ProcessDateFolder(datePath, roomCd, allFiles);
                });

                var results = OrderResults(allFiles.Values);
                _logger.LogInformation("Found {Count} files", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to find files");
                throw;
            }
        }

        private void ProcessDateFolder(string datePath, string roomCd, ConcurrentDictionary<string, FileMetadataDto> allFiles)
        {
            _logger.LogDebug("Processing date folder: {Path}", datePath);

            var files = _fileSystemClient.ListFilesAsync(datePath, roomFilter: roomCd).GetAwaiter().GetResult();
            
            if (files.Count == 0)
            {
                _logger.LogDebug("No files found in date folder: {Path}", datePath);
                return;
            }

            _logger.LogDebug("Retrieved {Count} files from {Path}", files.Count, datePath);

            foreach (var file in files)
            {
                var dto = CreateFileMetadataDto(file);
                if (!allFiles.ContainsKey(dto.AbsolutePath))
                {
                    allFiles[dto.AbsolutePath] = dto;
                }
            }
        }

        private FileMetadataDto CreateFileMetadataDto(SmbFileInfo file)
        {
            return new FileMetadataDto(
                FileName: file.FileName,
                Extension: file.Extension,
                SizeBytes: file.SizeBytes,
                CreatedUtc: file.CreatedUtc,
                AbsolutePath: file.FullPath,
                MatchedRoomFolder: file.RelativeDirectory?.Split('/', '\\').FirstOrDefault()
            );
        }

        private List<string> GetCandidateDatePaths(string locationPath, DateOnly date)
        {
            return _options.DateFolderFormats
                .Select(format => Path.Combine(locationPath, date.ToString(format)).Replace('\\', '/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public async Task<FileStreamResponse> OpenFileAsync(string absolutePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(absolutePath))
                {
                    throw new ArgumentException("Absolute path is required", nameof(absolutePath));
                }

                _logger.LogInformation("Opening file: {Path}", absolutePath);

                var stream = await _fileSystemClient.OpenFileAsync(absolutePath);
                var fileName = Path.GetFileName(absolutePath);

                if (!_contentTypeProvider.TryGetContentType(fileName, out var contentType))
                {
                    contentType = "application/octet-stream";
                }

                var response = new FileStreamResponse(stream, fileName, contentType);

                _logger.LogInformation("Successfully opened file: {FileName}, size: {Size} bytes", 
                    fileName, response.SizeBytes);

                return response;
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("File not found: {Path}", absolutePath);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open file: {Path}", absolutePath);
                throw;
            }
        }

        private IReadOnlyList<FileMetadataDto> OrderResults(IEnumerable<FileMetadataDto> results)
        {
            return results
                .OrderByDescending(f => !string.IsNullOrEmpty(f.MatchedRoomFolder)) // Files with rooms first
                .ThenBy(f => f.MatchedRoomFolder ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.AbsolutePath, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }
}
