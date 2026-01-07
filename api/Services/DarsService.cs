using DARSCommon.Clients.LogNotesServices;
using DARSCommon.Models;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;
using Scv.Core.Helpers.Extensions;
using Scv.Api.Models.Dars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Scv.Models.Dars;

namespace Scv.Api.Services
{
    public interface IDarsService
    {
        Task<DarsClientSearchResult> DarsApiSearch(DateTime date, string agencyIdentifierCd, string courtRoomCd);
        Task<IEnumerable<TranscriptDocument>> GetCompletedDocuments(string physicalFileId, string mdocJustinNo, bool returnChildRecords);
    }

    public class DarsService(
        IConfiguration configuration,
        ILogger<DarsService> logger,
        LogNotesServicesClient logNotesServicesClient,
        DARSCommon.Clients.TranscriptsServices.TranscriptsServicesClient transcriptsServicesClient,
        IMapper mapper) : IDarsService
    {

        #region Variables
        private readonly string _logsheetBaseUrl = configuration.GetNonEmptyValue("DARS:LogsheetUrl");

        #endregion Variables

        public async Task<DarsClientSearchResult> DarsApiSearch(DateTime date, string agencyIdentifierCd, string courtRoomCd)
        {
            logger.LogInformation("DarsApiSearch called for AgencyIdentifierCd: {AgencyIdentifierCd}, CourtRoomCd: {CourtRoomCd}, Date: {Date}", agencyIdentifierCd, courtRoomCd, date.ToString("yyyy-MM-dd"));

            if (agencyIdentifierCd == null)
            {
                logger.LogWarning("Location not found for AgencyIdentifierCd: {AgencyIdentifierCd}", agencyIdentifierCd);
                return new DarsClientSearchResult() { Results = [], Cookies = [] };
            }

            if (!int.TryParse(agencyIdentifierCd, out var agencyIdentifier))
            {
                logger.LogWarning("Unable to parse agencyIdentifierCd '{AgencyIdentifierCd}'", agencyIdentifierCd);
                return new DarsClientSearchResult() { Results = [], Cookies = [] };
            }

            var darsResult = await logNotesServicesClient.GetBaseAsync(room: courtRoomCd, datetime: date, location: agencyIdentifier, region: "all");
            logger.LogInformation("DarsApiSearch returned {ResultCount} results for AgencyIdentifier: {AgencyIdentifier}, CourtRoomCd: {CourtRoomCd}, Date: {Date}",
                darsResult?.Result?.Count ?? 0, agencyIdentifier, courtRoomCd, date.ToString("yyyy-MM-dd"));

            var mappedResults = mapper.Map<IEnumerable<DarsSearchResults>>(darsResult?.Result).ToList();

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
            List<SetCookieHeaderValue> cookies = [];

            if (darsResult?.Headers.TryGetValue("Set-Cookie", out var setCookieValues) == true)
            {
                cookies = ConvertSetCookieHeadersToCookieHeaderValues(setCookieValues);
                logger.LogDebug("Received {CookieCount} cookies from DARS API", cookies.Count);
            }


            return new DarsClientSearchResult() { Results = darsResultsPerRoom, Cookies = cookies };
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

        private static List<SetCookieHeaderValue> ConvertSetCookieHeadersToCookieHeaderValues(IEnumerable<string> setCookieHeaders)
        {
            var cookieHeaderValues = new List<SetCookieHeaderValue>();

            foreach (var setCookieHeader in setCookieHeaders)
            {
                if (SetCookieHeaderValue.TryParse(setCookieHeader, out var cookieHeaderValue))
                {
                    cookieHeaderValues.Add(cookieHeaderValue);
                }
            }

            return cookieHeaderValues;
        }

        public async Task<IEnumerable<TranscriptDocument>> GetCompletedDocuments(
            string physicalFileId,
            string mdocJustinNo,
            bool returnChildRecords)
        {
            logger.LogInformation(
                "GetCompletedDocuments called - PhysicalFileId: {PhysicalFileId}, MdocJustinNo: {MdocJustinNo}, ReturnChildRecords: {ReturnChildRecords}",
                physicalFileId,
                mdocJustinNo,
                returnChildRecords);

            var result = await transcriptsServicesClient.GetCompletedDocumentsBaseAsync(
                mdocJustinNo: mdocJustinNo,
                physicalFileId: physicalFileId,
                returnchildrecords: returnChildRecords);

            logger.LogInformation(
                "GetCompletedDocuments returned {ResultCount} documents",
                result?.Result?.Count ?? 0);

            var mappedDocuments = mapper.Map<IEnumerable<TranscriptDocument>>(result?.Result);
            return mappedDocuments ?? Enumerable.Empty<TranscriptDocument>();
        }
    }
}