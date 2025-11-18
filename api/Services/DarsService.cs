using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Scv.Api.Helpers;

namespace Scv.Api.Services
{
    public interface IDarsService
    {
        Task<IEnumerable<DarsSearchResults>> DarsApiSearch(DateTime date, string agencyIdentifierCd, string courtRoomCd);
    }
    public class DarsService(
        IConfiguration configuration,
        ILogger<DarsService> logger,
        LogNotesServicesClient logNotesServicesClient,
        IMapper mapper) : IDarsService
    {

        #region Variables
        private readonly string _logsheetBaseUrl = configuration.GetNonEmptyValue("DARS:LogsheetUrl");

        #endregion Variables

        public async Task<IEnumerable<DarsSearchResults>> DarsApiSearch(DateTime date, string agencyIdentifierCd, string courtRoomCd)
        {
            logger.LogInformation("DarsApiSearch called for AgencyIdentifierCd: {AgencyIdentifierCd}, CourtRoomCd: {CourtRoomCd}, Date: {Date}", agencyIdentifierCd, courtRoomCd, date.ToString("yyyy-MM-dd"));

            if (agencyIdentifierCd == null)
            {
                logger.LogWarning("Location not found for AgencyIdentifierCd: {AgencyIdentifierCd}", agencyIdentifierCd);
                return [];
            }

            if (!int.TryParse(agencyIdentifierCd, out var agencyIdentifier))
            {
                logger.LogWarning("Unable to parse agencyIdentifierCd '{AgencyIdentifierCd}'", agencyIdentifierCd);
                return [];
            }

            var darsResult = await logNotesServicesClient.GetBaseAsync(room: courtRoomCd, datetime: date, location: agencyIdentifier, region: "all");
            logger.LogInformation("DarsApiSearch returned {ResultCount} results for AgencyIdentifier: {AgencyIdentifier}, CourtRoomCd: {CourtRoomCd}, Date: {Date}", 
                darsResult?.Count ?? 0, agencyIdentifier, courtRoomCd, date.ToString("yyyy-MM-dd"));
            var mappedResults = mapper.Map<IEnumerable<DarsSearchResults>>(darsResult).ToList();

            // Use LINQ's Select to append the base URL to each result's Url property
            var resultsWithUrl = mappedResults
                .Select(result =>
                {
                    if (!string.IsNullOrWhiteSpace(result.Url) && !string.IsNullOrWhiteSpace(_logsheetBaseUrl))
                    {
                        result.Url = $"{_logsheetBaseUrl.TrimEnd('/')}/{result.Url.TrimStart('/')}";
                    }
                    return result;
                })
                .ToList();

            var darsResultsPerRoom = GetResultPerRoom(resultsWithUrl);
            return darsResultsPerRoom;
        }

        // only return result for each room, preferring CCD json, then FLS, then CCD html.
        // Note: multiple logsheets should be returned if there are multiple (FLS only) logsheets. CODE ORIGINALLY FROM PCSS.
        private static List<DarsSearchResults> GetResultPerRoom(List<DarsSearchResults> allResults)
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