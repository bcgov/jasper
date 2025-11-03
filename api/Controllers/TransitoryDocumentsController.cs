using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models;
using Scv.Api.Services;
using Scv.Core.Helpers.Exceptions;
using Scv.Models;
using Scv.Models.Document;
using System;
using System.Linq;
using System.Threading.Tasks;
using TDCommon.Clients.DocumentsServices;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class TransitoryDocumentsController(
        ITransitoryDocumentsService transitiveDocumentsService,
        IKeycloakTokenService service,
        IDocumentMerger documentMerger, IOptions<TdApiOptions> tdApiOptions) : ControllerBase
    {
        private readonly TdApiOptions _tdApiOptions = tdApiOptions.Value;

        /// <summary>
        /// Returns a list of transitory documents for a given location, room, and date. from the P (or Q) drive
        /// </summary>
        /// <param name="locationId">The location's unique agencyId</param>
        /// <param name="roomCd">The room code within the location</param>
        /// <param name="date">The date of the activity</param>
        /// <returns>List of document metadata found at the location specified by these parameters</returns>
        [HttpGet]
        public async Task<ActionResult> GetDocuments([FromQuery] string locationId, [FromQuery] string roomCd, DateOnly date)
        {
            if (string.IsNullOrWhiteSpace(locationId))
            {
                throw new BadRequestException("locationId is required and must be non-empty.");
            }

            if (string.IsNullOrWhiteSpace(roomCd))
            {
                throw new BadRequestException("roomCd is required and must be non-empty.");
            }

            var bearer = await service.GetAccessTokenAsync();
            var result = await transitiveDocumentsService.ListSharedDocuments(bearer, locationId, roomCd, date.ToString("yyyy-MM-dd"));

            return Ok(result);
        }

        /// <summary>
        /// Download a single document, based on its metadata (likely returned from the GetDocuments endpoint).
        /// </summary>
        /// <param name="fileMetadata">The file metadata to be downloaded, must have the path and the file size populated at a minimum</param>
        /// <returns>A stream corresponding to the document's contents</returns>
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile([FromQuery] FileMetadataDto fileMetadata)
        {
            if (fileMetadata == null)
            {
                throw new BadRequestException("fileMetadata is required.");
            }

            if (string.IsNullOrWhiteSpace(fileMetadata.AbsolutePath))
            {
                throw new BadRequestException("AbsolutePath is required and must be non-empty.");
            }

            if (fileMetadata.SizeBytes < 0)
            {
                throw new BadRequestException("SizeBytes must be greater than or equal to 0.");
            }

            if (fileMetadata.SizeBytes > _tdApiOptions.MaxFileSize)
            {
                var maxSizeMB = _tdApiOptions.MaxFileSize / 1024.0 / 1024.0;
                throw new BadRequestException($"File size exceeds maximum allowed size of {maxSizeMB:F2} MB.");
            }

            var bearer = await service.GetAccessTokenAsync();
            var fileResponse = await transitiveDocumentsService.DownloadFile(bearer, fileMetadata.AbsolutePath);

            return File(fileResponse.Stream, fileResponse.ContentType, fileResponse.FileName, enableRangeProcessing: true);
        }

        /// <summary>
        /// Retrieve an array of documents based on a list of FileMetadataDto objects.
        /// </summary>
        /// <param name="request">array of document metadata</param>
        /// <returns>A single merged document to be viewed in nutrient</returns>
        [HttpPost("merge")]
        public async Task<ActionResult> MergePdfs([FromBody] MergePdfsRequest request)
        {
            if (request?.Files == null || request.Files.Length == 0)
            {
                throw new BadRequestException("files are required and must contain at least one file.");
            }

            long totalSize = 0;
            foreach (var file in request.Files)
            {
                if (string.IsNullOrWhiteSpace(file.AbsolutePath))
                {
                    throw new BadRequestException("All files must have a valid AbsolutePath.");
                }

                if (file.SizeBytes < 0)
                {
                    throw new BadRequestException("All files must have SizeBytes greater than or equal to 0.");
                }

                totalSize += file.SizeBytes;
            }

            // Check total size against limit
            if (totalSize > _tdApiOptions.MaxFileSize)
            {
                var maxSizeMB = _tdApiOptions.MaxFileSize / 1024.0 / 1024.0;
                var totalSizeMB = totalSize / 1024.0 / 1024.0;
                throw new BadRequestException($"Total file size ({totalSizeMB:F2} MB) exceeds maximum allowed size of {maxSizeMB:F2} MB.");
            }

            var bearer = await service.GetAccessTokenAsync();

            var documentRequests = request.Files.Select(f => f.AbsolutePath).Select(path => new PdfDocumentRequest
            {
                Type = DocumentType.TransitoryDocument,
                Data = new PdfDocumentRequestDetails
                {
                    Path = path,
                    BearerToken = bearer
                }
            }).ToArray();

            var result = await documentMerger.MergeDocuments(documentRequests);

            return Ok(result);
        }

        public class MergePdfsRequest
        {
            public FileMetadataDto[] Files { get; set; }
        }
    }
}