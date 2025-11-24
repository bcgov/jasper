using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using PCSSCommon.Clients.TimebankServices;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Infrastructure;
using Scv.Api.Models.Timebank;

namespace Scv.Api.Services;

public interface ITimebankService
{
    Task<OperationResult<TimebankSummaryDto>> GetTimebankSummaryForJudgeAsync(int period, int judgeId, bool? includeLineItems = null, CancellationToken cancellationToken = default);
    Task<OperationResult<VacationPayoutDto>> GetTimebankPayoutsForJudgesAsync(int period, int judgeId, DateTime? expiryDate, double rate, CancellationToken cancellationToken = default);
}

public class TimebankService : ServiceBase, ITimebankService
{
    private readonly TimebankServicesClient _timebankClient;
    private readonly ILogger<TimebankService> _logger;
    private readonly IMapper _mapper;

    public override string CacheName => nameof(TimebankService);

    public TimebankService(
        IAppCache cache,
        TimebankServicesClient timebankClient,
        ILogger<TimebankService> logger,
        IMapper mapper
    ) : base(cache)
    {
        _timebankClient = timebankClient;
        _logger = logger;
        _mapper = mapper;

        _timebankClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver
        {
            NamingStrategy = new CamelCaseNamingStrategy()
        };
    }

    #region Public Methods

    public async Task<OperationResult<TimebankSummaryDto>> GetTimebankSummaryForJudgeAsync(int period, int judgeId, bool? includeLineItems = null, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving timebank summary for judge {JudgeId}, period {Period}, includeLineItems: {IncludeLineItems}", judgeId, period, includeLineItems);

            var summary = await _timebankClient.GetTimebankSummaryForJudgeAsync(period, judgeId, includeLineItems, cancellationToken);

            if (summary == null)
            {
                _logger.LogWarning("Timebank summary returned null for judge {JudgeId}, period {Period}", judgeId, period);
                return OperationResult<TimebankSummaryDto>.Failure("No timebank summary data available.");
            }

            var dto = TimebankSummaryDto.FromTimebankSummary(summary);

            _logger.LogInformation("Successfully retrieved timebank summary for judge {JudgeId}, period {Period}", judgeId, period);
            return OperationResult<TimebankSummaryDto>.Success(dto);
        }
        catch (PCSSCommon.Clients.TimebankServices.ApiException apiEx)
        {
            if (apiEx.StatusCode == 204)
            {
                _logger.LogInformation("Timebank summary returned 204 No Content for judge {JudgeId}, period {Period}", judgeId, period);
                return OperationResult<TimebankSummaryDto>.Success(null);
            }

            _logger.LogError(apiEx, "API error retrieving timebank summary for judge {JudgeId}, period {Period}. Status: {StatusCode}, Response: {Response}",
                judgeId, period, apiEx.StatusCode, apiEx.Response);
            return OperationResult<TimebankSummaryDto>.Failure($"Failed to retrieve timebank summary. Status: {apiEx.StatusCode}");
        }
        catch (ArgumentNullException argEx)
        {
            _logger.LogError(argEx, "Argument validation error retrieving timebank summary: {Message}", argEx.Message);
            return OperationResult<TimebankSummaryDto>.Failure("Invalid parameters provided for timebank summary.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving timebank summary for judge {JudgeId}, period {Period}: {Message}",
                judgeId, period, ex.Message);
            return OperationResult<TimebankSummaryDto>.Failure("An unexpected error occurred while retrieving timebank summary.");
        }
    }

    public async Task<OperationResult<VacationPayoutDto>> GetTimebankPayoutsForJudgesAsync(int period, int judgeId, DateTime? expiryDate, double rate, CancellationToken cancellationToken = default)
    {
        try
        {
            // Format the expiry date to dd-MMM-yyyy format if provided
            string formattedExpiryDate = expiryDate?.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

            _logger.LogInformation("Retrieving timebank payout for judge {JudgeId}, period {Period}, expiryDate: {ExpiryDate}, rate: {Rate}",
                judgeId, period, formattedExpiryDate, rate);

            var payout = await _timebankClient.GetTimebankPayoutsForJudgesAsync(period, judgeId, formattedExpiryDate, rate, cancellationToken);

            if (payout == null)
            {
                _logger.LogWarning("Timebank payout returned null for judge {JudgeId}, period {Period}", judgeId, period);
                return OperationResult<VacationPayoutDto>.Failure("No timebank payout data available.");
            }

            var dto = _mapper.Map<VacationPayoutDto>(payout);

            _logger.LogInformation("Successfully retrieved timebank payout for judge {JudgeId}, period {Period}. Total payout: {TotalPayout}",
                judgeId, period, dto.TotalPayout);
            return OperationResult<VacationPayoutDto>.Success(dto);
        }
        catch (PCSSCommon.Clients.TimebankServices.ApiException apiEx)
        {
            _logger.LogError(apiEx, "API error retrieving timebank payout for judge {JudgeId}, period {Period}. Status: {StatusCode}, Response: {Response}",
                judgeId, period, apiEx.StatusCode, apiEx.Response);
            return OperationResult<VacationPayoutDto>.Failure($"Failed to retrieve timebank payout. Status: {apiEx.StatusCode}");
        }
        catch (ArgumentNullException argEx)
        {
            _logger.LogError(argEx, "Argument validation error retrieving timebank payout: {Message}", argEx.Message);
            return OperationResult<VacationPayoutDto>.Failure("Invalid parameters provided for timebank payout.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving timebank payout for judge {JudgeId}, period {Period}: {Message}",
                judgeId, period, ex.Message);
            return OperationResult<VacationPayoutDto>.Failure("An unexpected error occurred while retrieving timebank payout.");
        }
    }

    #endregion Public Methods
}
