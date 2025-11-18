using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.TransitoryDocuments;
using Scv.Api.Services;
using Scv.Models;
using Scv.Models.Document;
using System;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using System.Threading;
using FileMetadata = Scv.TdApi.Models.FileMetadataDto;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class TransitoryDocumentsController(
        ITransitoryDocumentsService transitiveDocumentsService,
        IKeycloakTokenService keycloakTokenService,
        IDocumentMerger documentMerger,
        IValidator<GetDocumentsRequest> getDocumentsValidator,
        IValidator<DownloadFileRequest> downloadFileValidator,
        IValidator<MergePdfsRequest> mergePdfsValidator) : ControllerBase
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
        public async Task<ActionResult> GetDocuments(
            [FromQuery] string locationId,
            [FromQuery] string roomCd,
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            var request = new GetDocumentsRequest
            {
                LocationId = locationId,
                RoomCd = roomCd,
                Date = date
            };

            var validationResult = await _getDocumentsValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var result = await transitiveDocumentsService.ListSharedDocuments(request.LocationId, request.RoomCd, request.Date.ToString("yyyy-MM-dd"));

            return Ok(result);
        }

        /// <summary>
        /// Download a single document, based on its metadata (likely returned from the GetDocuments endpoint).
        /// </summary>
        /// <param name="fileMetadata">The file metadata to be downloaded, must have the path and the file size populated at a minimum</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A stream corresponding to the document's contents</returns>
        [HttpGet("download")]
        public async Task<IActionResult> DownloadFile(
            [FromQuery] FileMetadata fileMetadata,
            CancellationToken cancellationToken = default)
        {
            var request = new DownloadFileRequest
            {
                FileMetadata = fileMetadata
            };

            var validationResult = await _downloadFileValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var fileResponse = await transitiveDocumentsService.DownloadFile(request.FileMetadata.AbsolutePath);

            return File(fileResponse.Stream, fileResponse.ContentType, fileResponse.FileName, enableRangeProcessing: true);
        }

        /// <summary>
        /// Retrieve an array of documents based on a list of FileMetadataDto objects.
        /// </summary>
        /// <param name="request">array of document metadata</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A single merged document to be viewed in nutrient</returns>
        [HttpPost("merge")]
        public async Task<ActionResult> MergePdfs(
            [FromBody] MergePdfsRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationResult = await _mergePdfsValidator.ValidateAsync(request, cancellationToken);

            if (!validationResult.IsValid)
            {
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            var bearer = await keycloakTokenService.GetAccessTokenAsync();

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
    }
}