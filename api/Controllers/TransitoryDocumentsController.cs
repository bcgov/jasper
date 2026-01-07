using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Core.Helpers;
using Scv.Db.Models;
using Scv.Models;
using Scv.Models.Document;
using Scv.Models.TransitoryDocuments;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using FileMetadata = Scv.TdApi.Models.FileMetadataDto;

#pragma warning disable S6960 // Despite the warning, the merge endpoint is related to this controller and should not be split out.

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class TransitoryDocumentsController(
        ITransitoryDocumentsService transitiveDocumentsService,
        IDocumentMerger documentMerger,
        IValidator<GetDocumentsRequest> getDocumentsValidator,
        IValidator<DownloadFileRequest> downloadFileValidator,
        IValidator<MergePdfsRequest> mergePdfsValidator,
        ILogger<TransitoryDocumentsController> logger) : ControllerBase
    {
        private readonly IValidator<GetDocumentsRequest> _getDocumentsValidator = getDocumentsValidator;
        private readonly IValidator<DownloadFileRequest> _downloadFileValidator = downloadFileValidator;
        private readonly IValidator<MergePdfsRequest> _mergePdfsValidator = mergePdfsValidator;

        /// <summary>
        /// Returns a list of transitory documents for a given location, room, and date. from the P (or Q) drive
        /// </summary>
        /// <param name="locationId">The location's unique agencyId</param>
        /// <param name="roomCd">The room code within the location</param>
        /// <param name="date">The date of the activity</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of document metadata found at the location specified by these parameters</returns>
        [HttpGet]
        [RequiresPermission(permissions: Permission.LIST_TRANSITORY_DOCUMENTS)]
        public async Task<ActionResult> DocumentGet(
            [FromQuery] string locationId,
            [FromQuery] string roomCd,
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            var sanitizedLocationId = StringSanitizer.Sanitize(locationId);
            var sanitizedRoomCd = StringSanitizer.Sanitize(roomCd);

            logger.LogInformation(
                "Transitory documents list requested - LocationId: {LocationId}, RoomCd: {RoomCd}, Date: {Date}",
                sanitizedLocationId,
                sanitizedRoomCd,
                date);

            var request = new GetDocumentsRequest
            {
                LocationId = sanitizedLocationId,
                RoomCd = sanitizedRoomCd,
                Date = date
            };

            var validationResult = await _getDocumentsValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                logger.LogWarning(
                    "Invalid transitory documents request - Errors: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var result = await transitiveDocumentsService.ListSharedDocuments(
                request.LocationId,
                request.RoomCd,
                request.Date.ToString("yyyy-MM-dd"));

            logger.LogInformation(
                "Found {Count} transitory document(s) - LocationId: {LocationId}, RoomCd: {RoomCd}, Date: {Date}",
                result?.Count() ?? 0,
                sanitizedLocationId,
                sanitizedRoomCd,
                date);

            return Ok(result);
        }

        /// <summary>
        /// Download a single document, based on its metadata (likely returned from the DocumentGet endpoint).
        /// </summary>
        /// <param name="fileMetadata">The file metadata to be downloaded, must have the path and the file size populated at a minimum</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A stream corresponding to the document's contents</returns>
        [HttpGet("download")]
        [RequiresPermission(permissions: Permission.DOWNLOAD_TRANSITORY_DOCUMENTS)]
        public async Task<IActionResult> DocumentDownload(
            [FromQuery] FileMetadata fileMetadata,
            CancellationToken cancellationToken = default)
        {
            if (fileMetadata != null)
            {
                fileMetadata.FileName = StringSanitizer.Sanitize(fileMetadata.FileName);
                fileMetadata.Extension = StringSanitizer.Sanitize(fileMetadata.Extension);
                fileMetadata.RelativePath = StringSanitizer.Sanitize(fileMetadata.RelativePath);
                fileMetadata.MatchedRoomFolder = StringSanitizer.Sanitize(fileMetadata.MatchedRoomFolder);
            }

            logger.LogInformation(
                "Transitory document download requested - Path: {Path}, FileName: {FileName}",
                fileMetadata?.RelativePath,
                fileMetadata?.FileName);

            var request = new DownloadFileRequest
            {
                FileMetadata = fileMetadata
            };

            var validationResult = await _downloadFileValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                logger.LogWarning(
                    "Invalid download request - Errors: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var fileResponse = await transitiveDocumentsService.DownloadFile(request.FileMetadata.RelativePath);

            logger.LogInformation(
                "Transitory document downloaded successfully - Path: {Path}, FileName: {FileName}",
                fileMetadata?.RelativePath,
                fileMetadata?.FileName);

            return File(fileResponse.Stream, fileResponse.ContentType, fileResponse.FileName, enableRangeProcessing: true);
        }

        /// <summary>
        /// Retrieve an array of documents based on a list of FileMetadataDto objects.
        /// </summary>
        /// <param name="request">array of document metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A single merged document to be viewed in nutrient</returns>
        [HttpPost("merge")]
        [RequiresPermission(permissions: Permission.VIEW_TRANSITORY_DOCUMENTS)]
        public async Task<ActionResult> DocumentMerge(
            [FromBody] MergePdfsRequest request,
            CancellationToken cancellationToken = default)
        {
            // Sanitize file metadata strings in the request
            if (request?.Files != null)
            {
                foreach (var file in request.Files)
                {
                    file.FileName = StringSanitizer.Sanitize(file.FileName);
                    file.Extension = StringSanitizer.Sanitize(file.Extension);
                    file.RelativePath = StringSanitizer.Sanitize(file.RelativePath);
                    file.MatchedRoomFolder = StringSanitizer.Sanitize(file.MatchedRoomFolder);
                }
            }

            logger.LogInformation(
                "Transitory document merge requested - FileCount: {FileCount}",
                request?.Files?.Length ?? 0);

            var validationResult = await _mergePdfsValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                logger.LogWarning(
                    "Invalid merge request - Errors: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var documentRequests = request?.Files.Select(f => f.RelativePath).Select(path => new PdfDocumentRequest
            {
                Type = DocumentType.TransitoryDocument,
                Data = new PdfDocumentRequestDetails
                {
                    Path = path,
                }
            }).ToArray();

            var result = await documentMerger.MergeDocuments(documentRequests);

            logger.LogInformation(
                "Transitory documents merged successfully - FileCount: {FileCount}",
                request?.Files?.Length ?? 0);

            return Ok(result);
        }
    }
}

#pragma warning restore S6960
