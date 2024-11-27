using JCCommon.Clients.LocationServices;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using PCSSClient.Clients.PCSSLocationsServices;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;
using pcss_client.Models;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and PCSSLocationsServicesClient.
    /// </summary>
    public class LocationPCSSService
    {
        #region Variables

        private readonly IAppCache _cache;
        private readonly IConfiguration _configuration;
        private PCSSLocationsServicesClient _pcssLocationsClient { get; }

        #endregion Variables

        #region Properties

        #endregion Properties

        #region Constructor

        public LocationPCSSService(IConfiguration configuration, PCSSLocationsServicesClient pcssLocationsClient,
            IAppCache cache)
        {
            _configuration = configuration;
            _pcssLocationsClient = pcssLocationsClient;
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:LocationExpiryMinutes")) * 60;
            SetupLocationServicesClient();
        }

        #endregion Constructor

        #region Collection Methods

        public async Task<ICollection<PCSSLocation>> PCSSLocationsGetAsync()
        {
            var locations = await _pcssLocationsClient.LocationsGetAsync(CancellationToken.None);

            return locations;
        }


        #endregion Collection Methods

        #region Lookup Methods


        #endregion Lookup Methods

        #region Helpers
        private void SetupLocationServicesClient()
        {
            _pcssLocationsClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        }

        #endregion Helpers
    }
}