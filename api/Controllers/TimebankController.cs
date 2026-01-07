using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Scv.Api.Infrastructure.Authorization;
using Scv.Api.Services;
using Scv.Core.Helpers.Extensions;
using Scv.Db.Models;
using Scv.Models.Timebank;

namespace Scv.Api.Controllers;

[Authorize(AuthenticationSchemes = "SiteMinder, OpenIdConnect", Policy = nameof(ProviderAuthorizationHandler))]
[Route("api/[controller]")]
[ApiController]
public class TimebankController(
    ITimebankService timebankService,
    ILogger<TimebankController> logger,
    IValidator<TimebankSummaryRequest> summaryValidator,
    IValidator<TimebankPayoutRequest> payoutValidator) : ControllerBase
{
    private readonly ITimebankService _timebankService = timebankService;
    private readonly ILogger<TimebankController> _logger = logger;
    private readonly IValidator<TimebankSummaryRequest> _summaryValidator = summaryValidator;
    private readonly IValidator<TimebankPayoutRequest> _payoutValidator = payoutValidator;

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
    public async Task<ActionResult<Scv.Models.Timebank.TimebankSummaryDto>> GetTimebankSummaryForJudge(
        int period,
        [FromQuery] int? judgeId = null,
        [FromQuery] bool? includeLineItems = null,
        CancellationToken cancellationToken = default)
    {
        var request = new TimebankSummaryRequest
        {
            Period = period,
            JudgeId = judgeId,
            IncludeLineItems = includeLineItems
        };

        var validationResult = await _summaryValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid timebank summary request. Errors: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var resolvedJudgeId = User.JudgeId(request.JudgeId);

        _logger.LogInformation("Processing timebank summary request for judge {JudgeId}, period {Period}", resolvedJudgeId, request.Period);

        var result = await _timebankService.GetTimebankSummaryForJudgeAsync(request.Period, resolvedJudgeId, request.IncludeLineItems, cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to retrieve timebank summary for judge {JudgeId}, period {Period}. Errors: {Errors}",
                resolvedJudgeId, request.Period, string.Join(", ", result.Errors));
            return BadRequest(new { error = result.Errors });
        }

        _logger.LogInformation("Successfully retrieved timebank summary for judge {JudgeId}, period {Period}", resolvedJudgeId, request.Period);

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
    [RequiresPermission(permissions: Permission.VIEW_VACATION_PAYOUT)]
    public async Task<ActionResult<VacationPayoutDto>> GetTimebankPayoutsForJudges(
        int period,
        [FromQuery] int? judgeId = null,
        [FromQuery] DateTime? expiryDate = null,
        [FromQuery] double rate = 0,
        CancellationToken cancellationToken = default)
    {
        var request = new TimebankPayoutRequest
        {
            Period = period,
            JudgeId = judgeId,
            ExpiryDate = expiryDate,
            Rate = rate
        };

        var validationResult = await _payoutValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            _logger.LogWarning("Invalid timebank payout request. Errors: {Errors}",
                string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage)));
            return BadRequest(validationResult.Errors.Select(e => e.ErrorMessage));
        }

        var resolvedJudgeId = User.JudgeId(request.JudgeId);

        _logger.LogInformation("Processing timebank payout request for judge {JudgeId}, period {Period}, rate {Rate}, expiryDate {ExpiryDate}",
            resolvedJudgeId, request.Period, request.Rate, request.ExpiryDate?.ToString("yyyy-MM-dd"));

        var result = await _timebankService.GetTimebankPayoutsForJudgesAsync(request.Period, resolvedJudgeId, request.ExpiryDate, request.Rate, cancellationToken);

        if (!result.Succeeded)
        {
            _logger.LogWarning("Failed to retrieve timebank payout for judge {JudgeId}, period {Period}. Errors: {Errors}",
                resolvedJudgeId, request.Period, string.Join(", ", result.Errors));
            return BadRequest(new { error = result.Errors });
        }

        if (result.Payload == null)
        {
            _logger.LogWarning("Timebank payout returned null payload for judge {JudgeId}, period {Period}", resolvedJudgeId, request.Period);
            return NotFound(new { error = "Timebank payout not found." });
        }

        _logger.LogInformation("Successfully retrieved timebank payout for judge {JudgeId}, period {Period}. Total payout: {TotalPayout}",
            resolvedJudgeId, request.Period, result.Payload.TotalPayout);

        return Ok(result.Payload);
    }
}
