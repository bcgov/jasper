using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Models.Timebank;
using Scv.Api.Services;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class TimebankController(
    ITimebankService timebankService,
    ILogger<TimebankController> logger) : ControllerBase
{
    private readonly ITimebankService _timebankService = timebankService;
    private readonly ILogger<TimebankController> _logger = logger;

    /// <summary>
    /// Retrieves the timebank summary for a judge for a given period.
    /// </summary>
    /// <param name="period">The period identifier for the timebank summary.</param>
    /// <param name="judgeId">The judge ID. If not provided, uses the currently logged-in judge's ID.</param>
    /// <param name="includeLineItems">Optional flag to include line items in the summary.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Timebank summary record.</returns>
    [HttpGet]
    [Route("summary/{period}")]
    public async Task<ActionResult<TimebankSummaryDto>> GetTimebankSummaryForJudge(
        int period,
        [FromQuery] int? judgeId = null,
        [FromQuery] bool? includeLineItems = null,
        CancellationToken cancellationToken = default)
    {
        if (period <= 0)
        {
            _logger.LogWarning("Invalid period {Period} provided for timebank summary", period);
            return BadRequest(new { error = "Period must be a positive integer." });
        }

        var resolvedJudgeId = User.JudgeId(judgeId);

        if (resolvedJudgeId <= 0)
        {
            _logger.LogWarning("Invalid or missing judge ID for timebank summary request. Period: {Period}", period);
            return BadRequest(new { error = "A valid judge ID is required." });
        }

        _logger.LogInformation("Processing timebank summary request for judge {JudgeId}, period {Period}", resolvedJudgeId, period);

        var result = await _timebankService.GetTimebankSummaryForJudgeAsync(period, resolvedJudgeId, includeLineItems, cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to retrieve timebank summary for judge {JudgeId}, period {Period}. Errors: {Errors}",
                resolvedJudgeId, period, string.Join(", ", result.Errors));
            return BadRequest(new { error = result.Errors });
        }

        _logger.LogInformation("Successfully retrieved timebank summary for judge {JudgeId}, period {Period}", resolvedJudgeId, period);

        return Ok(result.Payload);
    }

    /// <summary>
    /// Calculates the vacation payout for a judge for a given period.
    /// </summary>
    /// <param name="period">The period identifier for the timebank payout.</param>
    /// <param name="judgeId">The judge ID. If not provided, uses the currently logged-in judge's ID.</param>
    /// <param name="expiryDate">The expiry date for vacation calculation. Optional.</param>
    /// <param name="rate">The payout rate to apply.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Vacation payout details.</returns>
    [HttpGet]
    [Route("payout/{period}")]
    public async Task<ActionResult<VacationPayoutDto>> GetTimebankPayoutsForJudges(
        int period,
        [FromQuery] int? judgeId = null,
        [FromQuery] DateTime? expiryDate = null,
        [FromQuery] double rate = 0,
        CancellationToken cancellationToken = default)
    {
        // Validate period
        if (period <= 1900)
        {
            _logger.LogWarning("Invalid period {Period} provided for timebank payout", period);
            return BadRequest(new { error = "Period must be a valid year." });
        }

        var resolvedJudgeId = User.JudgeId(judgeId);

        if (resolvedJudgeId <= 0)
        {
            _logger.LogWarning("Invalid or missing judge ID for timebank payout request. Period: {Period}", period);
            return BadRequest(new { error = "A valid judge ID is required." });
        }

        if (rate <= 0)
        {
            _logger.LogWarning("Invalid rate {Rate} provided for timebank payout. Judge: {JudgeId}, Period: {Period}",
                rate, resolvedJudgeId, period);
            return BadRequest(new { error = "Rate must be a positive number." });
        }

        _logger.LogInformation("Processing timebank payout request for judge {JudgeId}, period {Period}, rate {Rate}, expiryDate {ExpiryDate}",
            resolvedJudgeId, period, rate, expiryDate?.ToString("yyyy-MM-dd"));

        var result = await _timebankService.GetTimebankPayoutsForJudgesAsync(period, resolvedJudgeId, expiryDate, rate, cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to retrieve timebank payout for judge {JudgeId}, period {Period}. Errors: {Errors}",
                resolvedJudgeId, period, string.Join(", ", result.Errors));
            return BadRequest(new { error = result.Errors });
        }

        if (result.Payload == null)
        {
            _logger.LogWarning("Timebank payout returned null payload for judge {JudgeId}, period {Period}", resolvedJudgeId, period);
            return NotFound(new { error = "Timebank payout not found." });
        }

        _logger.LogInformation("Successfully retrieved timebank payout for judge {JudgeId}, period {Period}. Total payout: {TotalPayout}",
            resolvedJudgeId, period, result.Payload.TotalPayout);

        return Ok(result.Payload);
    }
}
