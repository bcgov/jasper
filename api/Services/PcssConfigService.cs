using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using PCSSCommon.Clients.ConfigurationServices;
using PCSSCommon.Clients.GlobalNonSittingDaysServicesClient;
using PCSSCommon.Models;
using Scv.Api.Helpers.Extensions;

namespace Scv.Api.Services;

public interface IPcssConfigService
{
    Task<ICollection<PcssConfiguration>> GetAllAsync();
    Task<int> GetLookAheadWindowAsync(DateTime startDate, string locationId = null);
}

public class PcssConfigService(
    IAppCache cache,
    ConfigurationServicesClient configClient,
    GlobalNonSittingDaysServicesClient nonSittingDaysServicesClient
    ) : ServiceBase(cache), IPcssConfigService
{
    public const string JUDGE_COURT_LIST_LOOKAHEAD_WINDOW = "JUDGE_COURT_LIST_LOOKAHEAD_WINDOW";
    public const string JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW = "JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW";
    public const string CIRCUIT_COURT_LOCATIONS = "CIRCUIT_COURT_LOCATIONS";

    private readonly ConfigurationServicesClient _configClient = configClient;
    private readonly GlobalNonSittingDaysServicesClient _nonSittingDaysServicesClient = nonSittingDaysServicesClient;

    public override string CacheName => nameof(PcssConfigService);

    public async Task<ICollection<PcssConfiguration>> GetAllAsync()
    {
        return await this.GetDataFromCache(this.CacheName, async () => await _configClient.GetAllAsync());
    }

    public async Task<int> GetLookAheadWindowAsync(DateTime startDate, string locationId = null)
    {
        var configData = await this.GetAllAsync();
        var lookAheadWindow = configData.GetIntValue(JUDGE_COURT_LIST_LOOKAHEAD_WINDOW);
        var specialLookAheadWindow = configData.GetIntValue(JUDGE_SPECIAL_COURT_LIST_LOOKAHEAD_WINDOW);
        var circuitCourtLocations = configData.GetListValue(CIRCUIT_COURT_LOCATIONS, removeEmptyEntries: true);
        var lookAhead = !string.IsNullOrWhiteSpace(locationId) && circuitCourtLocations.Contains(locationId)
            ? specialLookAheadWindow
            : lookAheadWindow;

        return await GetWorkingDaysLookahead(startDate, lookAhead);
    }

    #region Private Methods

    private async Task<int> GetWorkingDaysLookahead(DateTime startDate, int lookaheadWindow)
    {
        var endDate = startDate.AddDays(5 * lookaheadWindow);
        var nonSittingDays = await this.GetDataFromCache($"{this.CacheName}-NonSittingsDays-{startDate:yyyyMMdd}-{endDate:yyyyMMdd}",
            async () => await _nonSittingDaysServicesClient.GetAllAsync(startDate, endDate));
        var holidays = nonSittingDays.Where(nsd => nsd.ActivityType.ActivityCd == "HOL").Select(nsd => nsd.NonSittingDt?.Date);

        for (int dayWindow = 1; dayWindow <= lookaheadWindow; dayWindow++)
        {
            var currentDate = startDate.Date.AddDays(dayWindow);

            if (currentDate.DayOfWeek == DayOfWeek.Saturday || currentDate.DayOfWeek == DayOfWeek.Sunday || holidays.Contains(currentDate.Date))
            {
                lookaheadWindow++;
            }
        }

        return lookaheadWindow;
    }

    #endregion Private Methods
}
