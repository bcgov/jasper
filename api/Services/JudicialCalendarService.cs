using JCCommon.Clients.LocationServices;
using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using PCSSClient.Clients.JudicialCalendarsServices;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using System;
using System.Linq;
using System.Threading.Tasks;
using PCSS.Models.REST.JudicialCalendar;
using System.Collections.Generic;
using System.Threading;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and JudicialCalendarsServicesClient.
    /// </summary>
    public class JudicialCalendarService
    {
        #region Variables

        private readonly IAppCache _cache;
        private readonly IConfiguration _configuration;
        private JudicialCalendarsServicesClient _judicialCalendarsClient { get; }

        #endregion Variables

        #region Properties

        #endregion Properties

        #region Constructor

        public JudicialCalendarService(IConfiguration configuration, JudicialCalendarsServicesClient judicialCalendarsClient,
            IAppCache cache)
        {
            _configuration = configuration;
            _judicialCalendarsClient = judicialCalendarsClient;
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:LocationExpiryMinutes")) * 60;
            SetupLocationServicesClient();
        }

        #endregion Constructor

        #region Collection Methods

        public async Task<ICollection<JudicialCalendar>> JudicialCalendarsGetAsync(string locationId, DateTime startDate, DateTime endDate)
        {
            var judicialCalendars = await _judicialCalendarsClient.JudicialCalendarsGetAsync(locationId, startDate, endDate, CancellationToken.None);

            return judicialCalendars;
        }


        #endregion Collection Methods

        #region Lookup Methods


        #endregion Lookup Methods

        #region Helpers
        private void SetupLocationServicesClient()
        {
            _judicialCalendarsClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        }

        #endregion Helpers
    }
}