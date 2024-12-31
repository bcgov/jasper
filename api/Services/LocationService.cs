using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JCCommon.Clients.LocationServices;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Models.Location;
using PCSSLocationServices = PCSSCommon.Clients.LocationServices;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and LocationServicesClient.
    /// </summary>
    public class LocationService
    {
        #region Variables

        private readonly IAppCache _cache;
        private readonly IConfiguration _configuration;
        private readonly LocationServicesClient _locationClient;
        private readonly PCSSLocationServices.LocationServicesClient _pcssLocationServiceClient;

        #endregion Variables

        #region Properties

        #endregion Properties

        #region Constructor

        public LocationService(
            IConfiguration configuration,
            LocationServicesClient locationServicesClient,
            PCSSLocationServices.LocationServicesClient pcssLocationServiceClient,
            IAppCache cache
        )
        {
            _configuration = configuration;
            _locationClient = locationServicesClient;
            _pcssLocationServiceClient = pcssLocationServiceClient;
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(_configuration.GetNonEmptyValue("Caching:LocationExpiryMinutes")) * 60;
            SetupLocationServicesClient();
        }

        #endregion Constructor

        #region Collection Methods

        public async Task<ICollection<Location>> GetPCSSLocations() => await GetDataFromCache("PCSSLocations", async () =>
        {
            var locations = await _pcssLocationServiceClient.GetLocationsAsync();

            var mappedLocations = locations
                .Where(l => l.ActiveYn == "Y")
                .Select(l => new Location
                {
                    LocationId = l.LocationId.ToString(),
                    Name = l.LocationNm,
                    Code = l.LocationSNm
                })
                .OrderBy(l => l.Name)
                .ToList();

            return mappedLocations;
        });

        public async Task<ICollection<CodeValue>> GetLocations() => await GetDataFromCache("Locations", async () => await _locationClient.LocationsGetAsync(null, true, true));

        public async Task<ICollection<CodeValue>> GetCourtRooms()
        {
            return await GetDataFromCache($"Locations-Rooms", async () => await _locationClient.LocationsRoomsGetAsync());
        }

        #endregion Collection Methods

        #region Lookup Methods

        public async Task<string> GetLocationName(string code) => FindLongDescriptionFromCode(await GetLocations(), code);

        //Take the shortDesc -> translate it to code. 
        public async Task<string> GetLocationCodeFromId(string code)
        {
            var locations = await GetLocations();
            return locations.FirstOrDefault(loc => loc.ShortDesc == code)?.Code;
        }

        public async Task<string> GetLocationAgencyIdentifier(string code) => FindShortDescriptionFromCode(await GetLocations(), code);

        public async Task<string> GetRegionName(string code) => string.IsNullOrEmpty(code) ? null : await GetDataFromCache($"RegionNameByLocation-{code}", async () => (await _locationClient.LocationsRegionAsync(code))?.RegionName);

        #endregion Lookup Methods

        #region Helpers

        private async Task<T> GetDataFromCache<T>(string key, Func<Task<T>> fetchFunction)
        {
            return await _cache.GetOrAddAsync(key, async () => await fetchFunction.Invoke());
        }

        private string FindLongDescriptionFromCode(ICollection<CodeValue> lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.LongDesc;

        private string FindShortDescriptionFromCode(ICollection<CodeValue> lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.ShortDesc;

        private void SetupLocationServicesClient()
        {
            _locationClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        }

        #endregion Helpers
    }
}