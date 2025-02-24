using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using JCCommon.Clients.LocationServices;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Models.Location;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;
using PCSSLookupServices = PCSSCommon.Clients.LookupServices;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and LocationServicesClient.
    /// </summary>
    public class LocationService
    {
        #region Variables

        private readonly IAppCache _cache;
        private readonly LocationServicesClient _locationClient;
        private readonly PCSSLocationServices.LocationServicesClient _pcssLocationServiceClient;
        private readonly PCSSLookupServices.LookupServicesClient _pcssLookupServiceClient;
        private readonly IMapper _mapper;

        #endregion Variables

        #region Properties

        #endregion Properties

        #region Constructor

        public LocationService(
            IConfiguration configuration,
            LocationServicesClient locationServicesClient,
            PCSSLocationServices.LocationServicesClient pcssLocationServiceClient,
            PCSSLookupServices.LookupServicesClient pcssLookupServiceClient,
            IAppCache cache,
            IMapper mapper
        )
        {
            _locationClient = locationServicesClient;
            _pcssLocationServiceClient = pcssLocationServiceClient;
            _pcssLookupServiceClient = pcssLookupServiceClient;
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:LocationExpiryMinutes")) * 60;
            SetupLocationServicesClient();
            _mapper = mapper;
        }

        #endregion Constructor

        #region Collection Methods

        public async Task<ICollection<Location>> GetLocationsAndCourtRooms() => await GetDataFromCache("GetLocationsAndCourtRooms", async () =>
        {
            var getJCLocationsTask = this.GetJCLocationsAndCourtRooms();
            var getPCSSLocationsTask = this.GetPCSSLocationsWithCourtRooms();

            await Task.WhenAll(getJCLocationsTask, getPCSSLocationsTask);

            return MergeJCandPCSSLocations(getJCLocationsTask.Result, getPCSSLocationsTask.Result);
        });

        public async Task<ICollection<Location>> GetLocations() => await GetDataFromCache("Locations", async () =>
        {
            var getJCLocationsTask = this.GetJCLocations();
            var getPCSSLocationsTask = this.GetPCSSLocations();

            await Task.WhenAll(getJCLocationsTask, getPCSSLocationsTask);

            return MergeJCandPCSSLocations(getJCLocationsTask.Result, getPCSSLocationsTask.Result);
        });

        #endregion Collection Methods

        #region Lookup Methods

        public async Task<string> GetLocationName(string code) => FindLongDescriptionFromCode(await GetLocations(), code);

        //Take the shortDesc -> translate it to code. 
        public async Task<string> GetLocationCodeFromId(string code)
        {
            var locations = await GetLocations();
            return locations.FirstOrDefault(loc => loc.LocationId == code)?.Code;
        }

        public async Task<string> GetLocationAgencyIdentifier(string code) => FindShortDescriptionFromCode(await GetLocations(), code);

        public async Task<string> GetRegionName(string code) => string.IsNullOrEmpty(code) ? null : await GetDataFromCache($"RegionNameByLocation-{code}", async () => (await _locationClient.LocationsRegionAsync(code))?.RegionName);

        #endregion Lookup Methods


        #region JC Methods

        private async Task<ICollection<Location>> GetJCLocations()
        {
            var jcLocations = await _locationClient.LocationsGetAsync(null, true, true);

            var locations = _mapper.Map<List<Location>>(jcLocations);

            return locations;
        }

        private async Task<ICollection<Location>> GetJCLocationsAndCourtRooms()
        {
            var getLocationsTask = this.GetJCLocations();
            var getCourtRoomsTask = _locationClient.LocationsRoomsGetAsync();

            await Task.WhenAll(getLocationsTask, getCourtRoomsTask);

            var jcLocations = _mapper.Map<List<Location>>(getLocationsTask.Result);
            var jcCourtRooms = _mapper.Map<List<CourtRoom>>(getCourtRoomsTask.Result);

            foreach (var location in jcLocations)
            {
                location.CourtRooms = jcCourtRooms
                    .Where(cr => cr.LocationId == location.LocationId && (cr.Type == "CRT" || cr.Type == "HGR"))
                    .ToList();
            }

            return jcLocations;
        }

        #endregion JC Methods

        #region PCSS Methods

        private async Task<ICollection<Location>> GetPCSSLocations()
        {
            var pcssLocations = await _pcssLocationServiceClient.GetLocationsAsync();

            var locations = _mapper.Map<List<Location>>(pcssLocations);

            return locations;
        }

        private async Task<ICollection<Location>> GetPCSSLocationsWithCourtRooms()
        {
            // Both methods are of type PCSSCommon.Models.Location. The data coming from Location API has the "justAgenId"
            // and "locationSNm" field which is used to determine the equivalent location in JC while the Lookup API has the court rooms.
            var getLocationsTask = this.GetPCSSLocations();
            var getCourtRoomsTask = _pcssLookupServiceClient.GetCourtRoomsAsync();

            await Task.WhenAll(getLocationsTask, getCourtRoomsTask);

            var courtRooms = _mapper.Map<List<Location>>(getCourtRoomsTask.Result);

            foreach (var location in getLocationsTask.Result)
            {
                location.CourtRooms = courtRooms.Single(cr => cr.LocationId == location.LocationId).CourtRooms;
            }

            return getLocationsTask.Result;
        }

        #endregion PCSS Methods

        #region Helpers

        private static List<Location> MergeJCandPCSSLocations(ICollection<Location> jcLocations, ICollection<Location> pcssLocations)
        {
            var locations = jcLocations
                .Select(jc =>
                {
                    var match = pcssLocations.SingleOrDefault(pcss => pcss.JustinLocationId == jc.Code || pcss.Code == jc.LocationId);
                    return new Location
                    {
                        // JC
                        Name = jc.Name,
                        Code = jc.Code,
                        Active = jc.Active,

                        // PCSS
                        LocationId = match?.LocationId,
                        JustinLocationName = match?.JustinLocationName,
                        JustinLocationId = match?.JustinLocationId,
                        CourtRooms = match != null ? match.CourtRooms : jc.CourtRooms
                    };
                })
                .Where(l => l.Active.GetValueOrDefault())
                .OrderBy(l => l.Name)
                .ToList();

            return locations;
        }

        private async Task<T> GetDataFromCache<T>(string key, Func<Task<T>> fetchFunction)
        {
            return await _cache.GetOrAddAsync(key, async () => await fetchFunction.Invoke());
        }

        private static string FindLongDescriptionFromCode(ICollection<Location> lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.Name;

        private static string FindShortDescriptionFromCode(ICollection<Location> lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.Name;

        private void SetupLocationServicesClient()
        {
            _locationClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        }

        #endregion Helpers
    }
}