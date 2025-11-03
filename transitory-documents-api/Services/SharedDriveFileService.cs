using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Scv.Models;
using Scv.Models.TransitoryDocuments;
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
            TransitoryDocumentSearchRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);

            _logger.LogInformation(
                "Finding files for regionCode: {RegionCode}, agencyIdentifierCd: {AgencyIdentifierCd}, room: {Room}, date: {Date}",
                request?.RegionCode, request?.AgencyIdentifierCd, request?.RoomCd, request.Date);

            // Apply correction mappings: use regionCode/agencyIdentifierCd as target, get replacement folder name
            var regionFolderName = _correctionMappingOptions.RegionMappings
                .FirstOrDefault(m => string.Equals(request.RegionCode, m.Target, StringComparison.OrdinalIgnoreCase))
                ?.Replacement ?? request.RegionName;

            var locationFolderName = _correctionMappingOptions.LocationMappings
                .FirstOrDefault(m => string.Equals(request.AgencyIdentifierCd, m.Target, StringComparison.OrdinalIgnoreCase))
                ?.Replacement ?? request.LocationShortName;

            _logger.LogDebug(
                "Mapped regionCode '{RegionCode}' to folder '{RegionFolder}', agencyIdentifierCd '{AgencyIdentifierCd}' to folder '{LocationFolder}'",
                request.RegionCode, regionFolderName, request.AgencyIdentifierCd, locationFolderName);


            var locationPath = Path.Combine(regionFolderName, locationFolderName).Replace('\\', '/');
            var candidateDatePaths = GetCandidateDatePaths(locationPath, request.Date);
            var allFiles = new ConcurrentDictionary<string, FileMetadataDto>(StringComparer.OrdinalIgnoreCase);

            Parallel.ForEach(candidateDatePaths, async datePath =>
            {
            _logger.LogInformation("Searching date path: {Path}", datePath);
                await ProcessDateFolder(datePath, request.RoomCd, allFiles);
            });

            var results = OrderResults(allFiles.Values);
            _logger.LogInformation("Found {Count} files", results.Count);
            return results;
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
            catch (IOException ex)
            {
                _logger.LogWarning(ex, "File not found: {Path}", absolutePath);
                throw new IOException($"Error opening file at path '{absolutePath}'. See inner exception for details.", ex);
            }

        }

        private async Task ProcessDateFolder(string datePath, string? roomCd, ConcurrentDictionary<string, FileMetadataDto> allFiles)
        {
            _logger.LogDebug("Processing date folder: {Path}", datePath);

            var files = await _fileSystemClient.ListFilesAsync(datePath, roomFilter: roomCd);

            if (files.Count == 0)
            {
                _logger.LogDebug("No files found in date folder: {Path}", datePath);
                return;
            }

            _logger.LogDebug("Retrieved {Count} files from {Path}", files.Count, datePath);

            foreach (var file in files)
            {
                _logger.LogDebug("Processing file: {FileName} at {FullPath} relative directory {RelativeDirectory}", file.FileName, file.FullPath, file.RelativeDirectory);
                var dto = CreateFileMetadataDto(file);
                if (!allFiles.ContainsKey(dto.AbsolutePath))
                {
                    allFiles[dto.AbsolutePath] = dto;
                }
            }
        }

        private FileMetadataDto CreateFileMetadataDto(SmbFileInfo file)
        {
            return new FileMetadataDto()
            {
                FileName = file.FileName,
                Extension = file.Extension,
                SizeBytes = file.SizeBytes,
                CreatedUtc = file.CreatedUtc,
                AbsolutePath = file.FullPath,
                MatchedRoomFolder = file.RelativeDirectory?.Split(Path.DirectorySeparatorChar).FirstOrDefault()
            };
        }

        private List<string> GetCandidateDatePaths(string locationPath, DateOnly date)
        {
            return _options.DateFolderFormats
                .Select(format => Path.Combine(locationPath, date.ToString(format)).Replace('\\', '/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
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
