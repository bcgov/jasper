using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.TdApi.Infrastructure.Authorization;
using Scv.TdApi.Models;
using Scv.TdApi.Services;
using System.Globalization;

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
        /// Lists files for a region, location and date. Also scans any subfolders whose name CONTAINS the provided roomCode (case-insensitive).
        /// Returns absolute paths and the matched room folder name (if any).
        /// </summary>
        /// <remarks>
        /// - Searches: &lt;base&gt;\{region}\{location}\{dateFolder}\ and &lt;base&gt;\{region}\{location}\{dateFolder}\{*room*}
        /// - Date must be ISO: YYYY-MM-DD
        /// - Date folder name is resolved using all formats configured in SharedDrive:DateFolderFormats
        /// - Requires 'query' role
        /// </remarks>
        [HttpGet("search")]
        [Authorize(Policy = TdPolicies.RequireQueryRole)]
        [ProducesResponseType(typeof(IReadOnlyList<FileMetadataDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(string), StatusCodes.Status403Forbidden)]
        public IActionResult Search(
            [FromQuery] string region,
            [FromQuery] string location,
            [FromQuery] string roomCd,
            [FromQuery] string date)
        {
            if (string.IsNullOrWhiteSpace(region))
            {
                return BadRequest("region is required.");
            }
            if (string.IsNullOrWhiteSpace(location))
            {
                return BadRequest("location is required.");
            }
            if (string.IsNullOrWhiteSpace(roomCd))
            {
                return BadRequest("roomCd is required.");
            }
            if (!DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                return BadRequest("date must be in format yyyy-MM-dd.");
            }

            _logger.LogInformation(
                "File search requested region: {Region}, location: {Location}, room: {Room}, date: {Date}",
                region, location, roomCd, date);

            var foundFiles = _sharedDriveFileService.FindFilesAsync(region, location, roomCd, day);

            _logger.LogInformation(
                "File search completed found {FileCount} files",
                foundFiles.Count);

            return Ok(foundFiles);
        }

        /// <summary>
        /// Streams the specified file by ABSOLUTE path. The path must reside under the configured base path.
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
            if (string.IsNullOrWhiteSpace(path))
                return BadRequest("path is required and must be an absolute path.");

            _logger.LogInformation(
                "File content requested path: {Path}",
                path);

            var fileResponse = await _sharedDriveFileService.OpenFileAsync(path);

            _logger.LogInformation(
                "File content retrieved file: {FileName}, size: {Size} bytes",
                fileResponse.FileName, fileResponse.SizeBytes);

            return File(fileResponse.Stream, fileResponse.ContentType, fileResponse.FileName, enableRangeProcessing: true);
        }
    }
}
