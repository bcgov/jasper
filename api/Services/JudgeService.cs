using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LazyCache;
using PCSSCommon.Clients.PersonServices;
using PCSSCommon.Models;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models;

namespace Scv.Api.Services;


public interface IJudgeService
{
    Task<IEnumerable<PersonSearchItem>> GetJudges(List<string> positionCodes = null);
    Task<Person> GetJudge(int judgeId);
}

public class JudgeService(
        IAppCache cache,
        LocationService locationService,
        PersonServicesClient personClient
    ) : ServiceBase(cache), IJudgeService
{
    private readonly LocationService _locationService = locationService;
    private readonly PersonServicesClient _personClient = personClient;
    public override string CacheName => nameof(DashboardService);

    public const string CHIEF_JUDGE = "CJ";
    public const string ASSOC_CHIEF_JUDGE = "ACJ";
    public const string REGIONAL_ADMIN_JUDGE = "RAJ";
    public const string PUISNE_JUDGE = "PJ";
    public const string SENIOR_JUDGE = "SJ";

    public async Task<IEnumerable<PersonSearchItem>> GetJudges(List<string> positionCodes = null)
    {
        positionCodes ??= [];
        var date = DateTime.Now.ToClientTimezone().ToString("dd-MMM-yyyy");
        var locationsIds = (await _locationService.GetLocations()).Where(l => l.LocationId != null).Select(l => l.LocationId);

        async Task<ICollection<PersonSearchItem>> JudicialListing() => await _personClient.GetJudicialListingAsync(date, string.Join(",", locationsIds), false, "");
        var judicialListingTask = this.GetDataFromCache($"{JudicialListing}-{date}-{string.Join(",", locationsIds)}-{string.Join(",", positionCodes)}", JudicialListing);
        var judges = await judicialListingTask;

        // Filter by position codes if provided
        var filteredJudges = (positionCodes?.Count > 0)
            ? judges.Where(j => positionCodes.Contains(j.PositionCode))
            : judges;

        return filteredJudges.OrderBy(j => j.FullName);
    }

    public async Task<Person> GetJudge(int judgeId)
    {
        var judge = await _personClient.ReadPersonAsync(judgeId);
        var jsonString = Newtonsoft.Json.JsonConvert.SerializeObject(judge);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<Person>(jsonString);
    }
}
