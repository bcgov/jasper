using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Documents;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Models.Dars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Scv.Api.Controllers
{
    [Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
    [Route("api/[controller]")]
    [ApiController]
    public class DarsController(
        IDarsService darsService,
        ILogger<DarsController> logger,
        IValidator<TranscriptSearchRequest> transcriptSearchRequestValidator,
        IDocumentMerger documentMerger) : ControllerBase
    {
        /// <summary>
        /// Search for DARS audio recordings by date, location, and court room.
        /// </summary>
        /// <param name="date">The date to search for recordings</param>
        /// <param name="agencyIdentifierCd">The location ID</param>
        /// <param name="courtRoomCd">The court room code</param>
        /// <returns>A collection of audio recording log notes</returns>
        [HttpGet("search")]
        [ProducesResponseType(typeof(IEnumerable<DarsSearchResults>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> Search(DateTime date, string agencyIdentifierCd, string courtRoomCd)
        {
            var sanitizedCourtRoomCd = courtRoomCd?.Replace(Environment.NewLine, "").Trim();
            var sanitizedAgencyIdentifierCd = agencyIdentifierCd?.Replace(Environment.NewLine, "").Trim();
            logger.LogInformation(
                "DARS search requested - Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                date,
                sanitizedAgencyIdentifierCd,
                sanitizedCourtRoomCd
            );

            if (string.IsNullOrWhiteSpace(sanitizedAgencyIdentifierCd))
            {
                logger.LogWarning("Invalid agencyIdentifierCd provided: {AgencyIdentifierCd}", sanitizedAgencyIdentifierCd);
                return BadRequest("agencyIdentifierCd must be non-empty.");
            }

            try
            {
                var darsApiResult = await darsService.DarsApiSearch(date, sanitizedAgencyIdentifierCd, sanitizedCourtRoomCd);
                var searchResults = darsApiResult?.Results;

                if (searchResults == null || !searchResults.Any())
                {
                    logger.LogInformation(
                        "No DARS recordings found for Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                        date,
                        sanitizedAgencyIdentifierCd,
                        sanitizedCourtRoomCd
                    );
                    return NotFound();
                }

                AppendCookiesToResponse(darsApiResult.Cookies);

                logger.LogInformation(
                    "Found {Count} DARS recording(s) for Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}",
                    searchResults.Count(),
                    date,
                    sanitizedAgencyIdentifierCd,
                    sanitizedCourtRoomCd
                );

                return Ok(searchResults);
            }
            catch (ApiException ex)
            {
                logger.LogError(
                    ex,
                    "DARS API exception while searching - Date: {Date}, LocationId: {LocationId}, CourtRoom: {CourtRoom}, Status: {StatusCode}",
                    date,
                    sanitizedAgencyIdentifierCd,
                    sanitizedCourtRoomCd,
                    ex.StatusCode
                );

                // Return 404 for not found from upstream API
                if (ex.StatusCode == 404)
                {
                    return NotFound();
                }

                return StatusCode(500, "An error occurred while searching for audio recordings.");
            }
        }

        private void AppendCookiesToResponse(IEnumerable<Microsoft.Net.Http.Headers.SetCookieHeaderValue> cookies)
        {
            if (cookies == null || !cookies.Any())
            {
                return;
            }

            foreach (var cookie in cookies)
            {
                Response.Cookies.Append(cookie.Name.Value, cookie.Value.Value, new CookieOptions
                {
                    Domain = cookie.Domain.Value,
                    Path = cookie.Path.Value,
                    Expires = cookie.Expires,
                    HttpOnly = true,
                    Secure = true,
                    SameSite = cookie.SameSite switch
                    {
                        Microsoft.Net.Http.Headers.SameSiteMode.Lax => SameSiteMode.Lax,
                        Microsoft.Net.Http.Headers.SameSiteMode.Strict => SameSiteMode.Strict,
                        Microsoft.Net.Http.Headers.SameSiteMode.None => SameSiteMode.None,
                        _ => SameSiteMode.Unspecified
                    }
                });
            }

            logger.LogDebug("Added {CookieCount} cookies to response", cookies.Count());
        }

        /// <summary>
        /// Get completed transcript documents by physical file ID or MDOC JUSTIN number.
        /// </summary>
        /// <param name="request">The physical file ID (numeric string)</param>
        /// <returns>A collection of completed transcript documents</returns>
        [HttpGet("transcripts")]
        [ProducesResponseType(typeof(IEnumerable<TranscriptDocument>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTranscripts([FromQuery] TranscriptSearchRequest request)
        {
            var validationResult = await transcriptSearchRequestValidator.ValidateAsync(request);

            var sanitizedPhysicalFileId = request.PhysicalFileId?.Replace(Environment.NewLine, "").Trim();
            var sanitizedMdocJustinNo = request.MdocJustinNo?.Replace(Environment.NewLine, "").Trim();

            if (!validationResult.IsValid)
            {
                logger.LogWarning(
                    "Invalid transcript search request - Errors: {Errors}",
                    string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
                return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
            }

            logger.LogInformation(
                "Transcript search requested - PhysicalFileId: {PhysicalFileId}, MdocJustinNo: {MdocJustinNo}, ReturnChildRecords: {ReturnChildRecords}",
                sanitizedPhysicalFileId,
                sanitizedMdocJustinNo,
                request.ReturnChildRecords);

            try
            {
                var result = await darsService.GetCompletedDocuments(
                    sanitizedPhysicalFileId,
                    sanitizedMdocJustinNo,
                    request.ReturnChildRecords);

                if (result == null || !result.Any())
                {
                    logger.LogInformation(
                        "No transcripts found - PhysicalFileId: {PhysicalFileId}, MdocJustinNo: {MdocJustinNo}",
                        sanitizedPhysicalFileId,
                        sanitizedMdocJustinNo);
                    return NotFound();
                }

                logger.LogInformation(
                    "Found {Count} transcript(s) - PhysicalFileId: {PhysicalFileId}, MdocJustinNo: {MdocJustinNo}",
                    result.Count(),
                    sanitizedPhysicalFileId,
                    sanitizedMdocJustinNo);

                return Ok(result);
            }
            catch (DARSCommon.Clients.TranscriptsServices.ApiException ex)
            {
                logger.LogError(
                    ex,
                    "Transcripts API exception - PhysicalFileId: {PhysicalFileId}, MdocJustinNo: {MdocJustinNo}, Status: {StatusCode}",
                    sanitizedPhysicalFileId,
                    sanitizedMdocJustinNo,
                    ex.StatusCode);

                if (ex.StatusCode == 404)
                {
                    return NotFound();
                }

                return StatusCode(500, "An error occurred while searching for transcripts.");
            }
        }

        /// <summary>
        /// Gets a transcript document as a base64 PDF.
        /// </summary>
        /// <param name="orderId">The transcript order ID</param>
        /// <param name="documentId">The transcript document ID</param>
        /// <returns>Base64-encoded PDF document</returns>
        [HttpGet("transcript/{orderId}/{documentId}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetTranscriptDocument(string orderId, string documentId)
        {
            var sanitizedOrderId = orderId?.Replace(Environment.NewLine, "").Trim();
            var sanitizedDocumentId = documentId?.Replace(Environment.NewLine, "").Trim();

            if (string.IsNullOrWhiteSpace(sanitizedOrderId) || string.IsNullOrWhiteSpace(sanitizedDocumentId))
            {
                return BadRequest("Order ID and Document ID are required.");
            }

            logger.LogInformation(
                "Transcript document requested - OrderId: {OrderId}, DocumentId: {DocumentId}",
                sanitizedOrderId,
                sanitizedDocumentId);

            try
            {
                var documentRequest = new Scv.Models.Document.PdfDocumentRequest
                {
                    Type = Scv.Models.DocumentType.Transcript,
                    Data = new Scv.Models.Document.PdfDocumentRequestDetails
                    {
                        OrderId = sanitizedOrderId,
                        DocumentId = sanitizedDocumentId
                    }
                };

                var result = await documentMerger.MergeDocuments([documentRequest]);

                logger.LogInformation(
                    "Transcript document retrieved successfully - OrderId: {OrderId}, DocumentId: {DocumentId}",
                    sanitizedOrderId,
                    sanitizedDocumentId);

                return Ok(new { base64Pdf = result.Base64Pdf });
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Error retrieving transcript document - OrderId: {OrderId}, DocumentId: {DocumentId}",
                    sanitizedOrderId,
                    sanitizedDocumentId);
                return StatusCode(500, "An error occurred while retrieving the transcript document.");
            }
        }
    }
}
