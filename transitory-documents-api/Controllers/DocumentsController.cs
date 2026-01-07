using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Core.Helpers;
using Scv.Models.TransitoryDocuments;
using Scv.TdApi.Infrastructure.Authorization;
using Scv.TdApi.Models;
using Scv.TdApi.Services;

namespace Scv.TdApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly ISharedDriveFileService _sharedDriveFileService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            ISharedDriveFileService files,
            ILogger<DocumentsController> logger)
        {
            _sharedDriveFileService = files;
            _logger = logger;
        }

        /// <summary>
        /// Lists files for a region, location and date. Also scans any subfolders whose name matches the provided roomCode.
        /// Returns relative paths and the matched room folder name (if any).
        /// </summary>
        /// <remarks>
        /// - Searches: &lt;base&gt;\{region}\{location}\{dateFolder}\ and &lt;base&gt;\{region}\{location}\{dateFolder}\{*room*}
        /// - Date must be ISO: YYYY-MM-DD
        /// - Date folder name is resolved using all formats configured in SharedDrive:DateFolderFormats
        /// - Requires 'query' role
        /// </remarks>
        [HttpPost("search")]
        [Authorize(Policy = TdPolicies.RequireQueryRole)]
        [ProducesResponseType(typeof(IReadOnlyList<FileMetadataDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Search([FromBody] TransitoryDocumentSearchRequest request)
        {
            if (request == null)
            {
                return BadRequest("Request body is required.");
            }

            request.RegionCode = StringSanitizer.Sanitize(request.RegionCode) ?? string.Empty;
            request.RegionName = StringSanitizer.Sanitize(request.RegionName) ?? string.Empty;
            request.AgencyIdentifierCd = StringSanitizer.Sanitize(request.AgencyIdentifierCd) ?? string.Empty;
            request.LocationShortName = StringSanitizer.Sanitize(request.LocationShortName) ?? string.Empty;
            request.RoomCd = StringSanitizer.Sanitize(request.RoomCd);

            _logger.LogInformation(
                "File search requested - RegionCode: {RegionCode}, RegionName: {RegionName}, AgencyIdentifierCd: {AgencyIdentifierCd}, LocationShortName: {LocationShortName}, Room: {Room}, Date: {Date}",
                request.RegionCode, request.RegionName, request.AgencyIdentifierCd, request.LocationShortName, request.RoomCd, request.Date);

            var foundFiles = await _sharedDriveFileService.FindFilesAsync(
                request);

            _logger.LogInformation(
                "File search completed found {FileCount} files",
                foundFiles.Count);

            return Ok(foundFiles);
        }

        /// <summary>
        /// Streams the specified file by relative path. The path must reside under the configured base path.
        /// </summary>
        [HttpGet("content")]
        [Authorize(Policy = TdPolicies.RequireReadRole)]
        [Produces("application/octet-stream", "application/pdf", "image/jpeg", "image/png")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetContent([FromQuery] string path)
        {
            var sanitizedPath = StringSanitizer.Sanitize(path);

            if (string.IsNullOrWhiteSpace(sanitizedPath))
                return BadRequest("path is required and must be an relative path.");

            _logger.LogInformation(
                "File content requested path: {Path}",
                sanitizedPath);

            var fileResponse = await _sharedDriveFileService.OpenFileAsync(sanitizedPath);

            _logger.LogInformation(
                "File content retrieved file: {FileName}, size: {Size} bytes",
                fileResponse.FileName, fileResponse.SizeBytes);

            return File(fileResponse.Stream, fileResponse.ContentType, fileResponse.FileName, enableRangeProcessing: true);
        }
    }
}
