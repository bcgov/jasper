using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JCCommon.Clients.LocationServices;
using LazyCache;
using MapsterMapper;
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

        public async Task<ICollection<Location>> GetLocations(bool includeChildRecords = false) => await GetDataFromCache($"Locations{includeChildRecords}", async () =>
        {
            var getJCLocationsTask = this.GetJCLocations(includeChildRecords);
            var getPCSSLocationsTask = this.GetPCSSLocations(includeChildRecords);

            await Task.WhenAll(getJCLocationsTask, getPCSSLocationsTask);

            return Models.Location.Locations.Create(getJCLocationsTask.Result, getPCSSLocationsTask.Result);
        });

        #endregion Collection Methods

        #region Lookup Methods

        public virtual async Task<string> GetLocationShortName(string locationId)
        {
            var locations = await GetLocations();
            return locations.FirstOrDefault(loc => loc.LocationId == locationId)?.ShortName;
        }

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

        private async Task<ICollection<Location>> GetJCLocations(bool includeChildRecords)
        {
            var jcLocations = await _locationClient.LocationsGetAsync(null, true, true);
            var locations = _mapper.Map<List<Location>>(jcLocations);

            if (!includeChildRecords)
            {
                return locations;
            }

            var jcCourtRooms = await _locationClient.LocationsRoomsGetAsync();
            var courtRooms = _mapper.Map<List<CourtRoom>>(jcCourtRooms);

            foreach (var location in locations)
            {
                location.CourtRooms = [.. courtRooms
                    .Where(cr => cr.LocationId == location.LocationId && (cr.Type == "CRT" || cr.Type == "HGR"))
                    .OrderBy(cr => cr.Room)];
            }

            return locations;
        }

        #endregion JC Methods

        #region PCSS Methods

        private async Task<ICollection<Location>> GetPCSSLocations(bool includeChildRecords)
        {
            var pcssLocations = await _pcssLocationServiceClient.GetLocationsAsync();
            var locations = _mapper.Map<List<Location>>(pcssLocations);

            if (!includeChildRecords)
            {
                return locations;
            }

            var pcssCourtRooms = await _pcssLookupServiceClient.GetCourtRoomsAsync();
            var courtRooms = _mapper.Map<List<Location>>(pcssCourtRooms);

            foreach (var location in locations)
            {
                location.CourtRooms = [.. courtRooms
                    .Single(cr => cr.LocationId == location.LocationId)
                    .CourtRooms
                    .OrderBy(cr => cr.Room)
                ];
            }

            return locations;
        }

        #endregion PCSS Methods

        #region Helpers

        private async Task<T> GetDataFromCache<T>(string key, Func<Task<T>> fetchFunction)
        {
            return await _cache.GetOrAddAsync(key, async () => await fetchFunction.Invoke());
        }

        private static string FindLongDescriptionFromCode(ICollection<Location> lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.Name;

        private static string FindShortDescriptionFromCode(ICollection<Location> lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.AgencyIdentifierCd;

        private void SetupLocationServicesClient()
        {
            _locationClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        }

        #endregion Helpers
    }
}