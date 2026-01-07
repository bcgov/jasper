using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using PCSSCommon.Clients.JudicialCalendarServices;
using PCSSCommon.Models;
using Scv.Core.Helpers.ContractResolver;
using Scv.Core.Helpers.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PCSSConstants = PCSSCommon.Common.Constants;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and JudicialCalendarsServicesClient.
    /// </summary>
    public class JudicialCalendarService
    {
        #region Variables

        private JudicialCalendarServicesClient _judicialCalendarsClient { get; }

        #endregion Variables

        #region Properties

        #endregion Properties

        #region Constructor

        public JudicialCalendarService(IConfiguration configuration, JudicialCalendarServicesClient judicialCalendarClient,
            IAppCache cache)
        {
            _judicialCalendarsClient = judicialCalendarClient;
            cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:LocationExpiryMinutes")) * 60;
            SetupLocationServicesClient();
        }

        #endregion Constructor

        #region Collection Methods

        public async Task<ICollection<JudicialCalendar>> JudicialCalendarsGetAsync(string locationIds, DateTime startDate, DateTime endDate)
        {
            var judicialCalendars = await _judicialCalendarsClient.ReadCalendarsV2Async(
               locationIds,
               startDate.ToString(PCSSConstants.DATE_FORMAT),
               endDate.ToString(PCSSConstants.DATE_FORMAT),
               string.Empty
            );

            return judicialCalendars.Calendars;
        }

        /// <summary>
        /// Retrieves the Judge's calendar based from start and end date.
        /// </summary>
        /// <param name="judgeId">The Judge's id</param>
        /// <param name="startDate">The start date</param>
        /// <param name="endDate">The end date</param>
        /// <returns>Judge's calendar based from start and end date</returns>
        public async Task<JudicialCalendar> GetJudgeCalendarAsync(int judgeId, DateTime startDate, DateTime endDate)
        {
            return await _judicialCalendarsClient.ReadCalendarV2Async(
                judgeId,
                startDate.ToString(PCSSConstants.DATE_FORMAT),
                endDate.ToString(PCSSConstants.DATE_FORMAT));
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