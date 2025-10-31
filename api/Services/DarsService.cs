using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;

namespace Scv.Api.Services
{
    public class DarsService
    {
        #region Variables

        private readonly ILogger<DarsService> _logger;
        private readonly LogNotesServicesClient _logNotesServicesClient;
        private readonly IAppCache _cache;
        private readonly IMapper _mapper;
        private readonly string _logsheetBaseUrl;
        private readonly LocationService _locationService;

        #endregion Variables

        #region Constructor

        public DarsService(
            IConfiguration configuration,
            ILogger<DarsService> logger,
            LogNotesServicesClient logNotesServicesClient,
            LocationService locationService,

            IMapper mapper,
            IAppCache cache)
        {
            _logger = logger;
            _cache = cache;
            _mapper = mapper;
            _logNotesServicesClient = logNotesServicesClient;
            _logsheetBaseUrl = configuration.GetNonEmptyValue("DARS:LogsheetUrl");
            _locationService = locationService;
        }

        #endregion Constructor

        public async Task<IEnumerable<DarsSearchResults>> DarsApiSearch(DateTime date, int locationId, string courtRoomCd)
        {
            var darsResult = await _logNotesServicesClient.GetBaseAsync(room: courtRoomCd, datetime: date, location: locationId);
            var mappedResults = _mapper.Map<IEnumerable<DarsSearchResults>>(darsResult).ToList();

            // Append the base URL to each result's Url property
            foreach (var result in mappedResults)
            {
                var locations = await _locationService.GetLocations();
                if (!string.IsNullOrWhiteSpace(result.Url) && !string.IsNullOrWhiteSpace(_logsheetBaseUrl))
                {
                    result.Url = $"{_logsheetBaseUrl.TrimEnd('/')}/{result.Url.TrimStart('/')}"; // remap the url returned from DARS - to point to the base url hosting the logsheet
                }
            }

            var darsResultsPerRoom = GetResultPerRoom(mappedResults);
            return darsResultsPerRoom;
        }

        // only return result for each room, preferring CCD json, then FLS, then CCD html.
        // Note: multiple logsheets should be returned if there are multiple (FLS only) logsheets. CODE ORIGINALLY FROM PCSS.
        private List<DarsSearchResults> GetResultPerRoom(List<DarsSearchResults> allResults)
        {
            var resultsForRoom = allResults.GroupBy(result => result.CourtRoomCd);
            List<DarsSearchResults> filteredResults = new List<DarsSearchResults>(allResults.Count);
            foreach (IGrouping<string, DarsSearchResults> roomResults in resultsForRoom)
            {
                var actualResult = roomResults.FirstOrDefault(roomResult => roomResult.FileName != null && roomResult.FileName.ToLowerInvariant().Contains("json"));
                if (actualResult != null)
                {
                    filteredResults.Add(actualResult);
                }
                var flsResults = roomResults.Where(roomResult => roomResult.FileName != null && roomResult.FileName.ToLowerInvariant().Contains("fls")).ToList();
                if (flsResults.Count >= 1)
                {
                    filteredResults.AddRange(flsResults);
                    continue;
                }
                if (actualResult == null)
                {
                    filteredResults.Add(roomResults.FirstOrDefault());
                }

            }
            return filteredResults;
        }
    }
}