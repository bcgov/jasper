using System.Collections.Concurrent;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;
using Scv.Core.Exceptions;
using Scv.Models;
using Scv.Models.Document;
using Scv.Models.TransitoryDocuments;
using Scv.TdApi.Infrastructure.FileSystem;
using Scv.TdApi.Infrastructure.Options;

namespace Scv.TdApi.Services
{
    public sealed class SharedDriveFileService(
        ISmbFileSystemClient fileSystemClient,
        ILogger<SharedDriveFileService> logger,
        IOptions<SharedDriveOptions> options,
        IOptions<CorrectionMappingOptions> correctionMappingOptions,
        IOptions<TdApiOptions> tdApiOptions) : ISharedDriveFileService
    {
        private static readonly SemaphoreSlim OpenFileConcurrencySemaphore = new(4, 4);
        private readonly ISmbFileSystemClient _fileSystemClient = fileSystemClient ?? throw new ArgumentNullException(nameof(fileSystemClient));
        private readonly ILogger<SharedDriveFileService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly FileExtensionContentTypeProvider _contentTypeProvider = new();
        private readonly SharedDriveOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        private readonly CorrectionMappingOptions _correctionMappingOptions = correctionMappingOptions?.Value ?? throw new ArgumentNullException(nameof(correctionMappingOptions));
        private readonly TdApiOptions _tdApiOptions = tdApiOptions?.Value ?? throw new ArgumentNullException(nameof(tdApiOptions));

        public async Task<IReadOnlyList<FileMetadataDto>> FindFilesAsync(
            TransitoryDocumentSearchRequest request,
            System.Threading.CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            cancellationToken.ThrowIfCancellationRequested();

            _logger.LogInformation(
                "Finding files for regionCode: {RegionCode}, agencyIdentifierCd: {AgencyIdentifierCd}, room: {Room}, date: {Date}",
                request.RegionCode, request.AgencyIdentifierCd, request.RoomCd, request.Date);

            // Apply correction mappings: use regionCode/agencyIdentifierCd as target, get replacement folder name
            var regionMapping = _correctionMappingOptions.RegionMappings
                .FirstOrDefault(m => string.Equals(request.RegionCode, m.Target, StringComparison.OrdinalIgnoreCase));
            var regionFolderName = regionMapping?.Replacement ?? request.RegionName;
            if (regionMapping is null)
            {
                SmbPathUtility.ValidatePathSegment(regionFolderName, nameof(request.RegionName));
            }

            CorrectionMapping? locationMapping;
            var effectiveRoomCd = request.RoomCd;

            if (_correctionMappingOptions.VirtualBailMappings.Any(vb => string.Equals(vb.Target, request.RoomCd, StringComparison.OrdinalIgnoreCase)))
            {
                locationMapping = _correctionMappingOptions.VirtualBailMappings
                    .FirstOrDefault(m => string.Equals(request.RoomCd, m.Target, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                locationMapping = _correctionMappingOptions.LocationMappings
                    .FirstOrDefault(m => string.Equals(request.AgencyIdentifierCd, m.Target, StringComparison.OrdinalIgnoreCase));
            }
            string locationFolderName = locationMapping?.Replacement ?? request.LocationShortName;
            if (locationMapping is null)
            {
                SmbPathUtility.ValidatePathSegment(locationFolderName, nameof(request.LocationShortName));
            }

            if (locationMapping?.IgnoreRoom == true)
            {
                effectiveRoomCd = null;
            }

            if (!string.IsNullOrWhiteSpace(effectiveRoomCd))
            {
                SmbPathUtility.ValidatePathSegment(effectiveRoomCd, nameof(request.RoomCd));
            }

            _logger.LogDebug(
                "Mapped regionCode '{RegionCode}' to folder '{RegionFolder}', agencyIdentifierCd '{AgencyIdentifierCd}' to folder '{LocationFolder}', roomCd to '{RoomCd}'",
                request.RegionCode, regionFolderName, request.AgencyIdentifierCd, locationFolderName, effectiveRoomCd);


            var locationPath = SmbPathUtility.CombinePathWithForwardSlashes(regionFolderName, locationFolderName);
            var candidateDatePaths = GetCandidateDatePaths(locationPath, request.Date);
            var allFiles = new ConcurrentDictionary<string, FileMetadataDto>(StringComparer.OrdinalIgnoreCase);

            // Process all date paths in parallel and await completion
            var tasks = candidateDatePaths.Select(async datePath =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                _logger.LogInformation("Searching date path: {Path}", datePath);
                await ProcessDateFolder(datePath, effectiveRoomCd, allFiles, cancellationToken);
            });

            await Task.WhenAll(tasks);

            var results = OrderResults(allFiles.Values);
            _logger.LogInformation("Found {Count} files", results.Count);
            return results;
        }

        public async Task<FileStreamResponse> OpenFileAsync(string relativePath, System.Threading.CancellationToken cancellationToken = default)
        {
            var semaphoreAcquired = false;
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                await OpenFileConcurrencySemaphore.WaitAsync(cancellationToken);
                semaphoreAcquired = true;

                if (string.IsNullOrWhiteSpace(relativePath))
                {
                    throw new ArgumentException("Relative path is required", nameof(relativePath));
                }

                SmbPathUtility.ValidateRelativePath(relativePath);

                _logger.LogInformation("Opening file: {Path}", relativePath);

                var fileInfo = await _fileSystemClient.GetFileInfoAsync(relativePath, cancellationToken);
                if (_tdApiOptions.MaxFileSize > 0 && fileInfo.SizeBytes > _tdApiOptions.MaxFileSize)
                {
                    var maxSizeMb = _tdApiOptions.MaxFileSize / 1024.0 / 1024.0;
                    throw new BadRequestException($"File size exceeds maximum allowed size of {maxSizeMb:F2} MB.");
                }

                var stream = await _fileSystemClient.OpenFileAsync(relativePath, cancellationToken);
                var fileName = Path.GetFileName(relativePath);

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
                _logger.LogWarning(ex, "IOException opening file: {Path}", relativePath);
                throw new IOException($"Error opening file at path '{relativePath}'. See inner exception for details.", ex);
            }
            finally
            {
                if (semaphoreAcquired)
                {
                    OpenFileConcurrencySemaphore.Release();
                }
            }

        }

        private string RemoveBasePath(string fullPath)
        {
            if (string.IsNullOrWhiteSpace(_options.BasePath))
                return fullPath;

            var normalizedFullPath = fullPath.Replace('/', '\\').Trim('\\');
            var normalizedBasePath = _options.BasePath.Replace('/', '\\').Trim('\\');

            if (normalizedFullPath.StartsWith(normalizedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                var pathWithoutBase = normalizedFullPath[normalizedBasePath.Length..].TrimStart('\\');
                _logger.LogDebug("Removed base path '{Base}' from '{Full}', result: '{Result}'",
                    normalizedBasePath, normalizedFullPath, pathWithoutBase);
                return pathWithoutBase;
            }

            return fullPath;
        }

        private async Task ProcessDateFolder(string datePath, string? roomCd, ConcurrentDictionary<string, FileMetadataDto> allFiles, System.Threading.CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _logger.LogInformation("Processing date folder: {Path}", datePath);

            try
            {
                var files = await _fileSystemClient.ListFilesAsync(datePath, roomFilter: roomCd, cancellationToken: cancellationToken);

                if (files.Count == 0)
                {
                    _logger.LogWarning("No files found in date folder: {Path}", datePath);
                    return;
                }

                _logger.LogDebug("Retrieved {Count} files from {Path}", files.Count, datePath);

                foreach (var file in files)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    _logger.LogDebug("Processing file: {FileName} at {FullPath} relative directory {RelativeDirectory}", file.FileName, file.FullPath, file.RelativeDirectory);
                    var dto = CreateFileMetadataDto(file);
                    var fileIdentity = CreateFileIdentityKey(dto.RelativePath, dto.FileName);
                    allFiles.TryAdd(fileIdentity, dto);
                }
            }
            catch (FileNotFoundException ex)
            {
                _logger.LogWarning(ex, "Date file not found: {Path}", datePath);
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogWarning(ex, "Date folder not found: {Path}", datePath);
            }
        }

        private FileMetadataDto CreateFileMetadataDto(SmbFileInfo file)
        {
            // Strip the base path from FullPath to create a relative path
            var relativePath = RemoveBasePath(file.FullPath);

            return new FileMetadataDto()
            {
                FileName = file.FileName,
                Extension = file.Extension,
                SizeBytes = file.SizeBytes,
                CreatedUtc = file.CreatedUtc,
                RelativePath = relativePath,
                MatchedRoomFolder = file.RelativeDirectory?.Split(Path.DirectorySeparatorChar).FirstOrDefault()
            };
        }

        private List<string> GetCandidateDatePaths(string locationPath, DateOnly date)
        {
            var paths = new List<string>();

            foreach (var format in _options.DateFolderFormats)
            {
                var formattedDate = date.ToString(format);

                var exactPath = SmbPathUtility.CombinePathWithForwardSlashes(locationPath, formattedDate);
                paths.Add(exactPath);
            }

            return [.. paths.Distinct(StringComparer.OrdinalIgnoreCase)];
        }

        private static List<FileMetadataDto> OrderResults(IEnumerable<FileMetadataDto> results)
        {
            return [.. results
                .OrderByDescending(f => !string.IsNullOrEmpty(f.MatchedRoomFolder)) // Files with rooms first
                .ThenBy(f => f.MatchedRoomFolder ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(f => f.RelativePath, StringComparer.OrdinalIgnoreCase)];
        }

        private static string CreateFileIdentityKey(string relativePath, string fileName)
        {
            return $"{relativePath}::{fileName}";
        }
    }
}
