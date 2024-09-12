using LazyCache;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JCCommon.Clients.LookupCodeServices;
using CodeLookup = System.Collections.Generic.ICollection<JCCommon.Clients.LookupCodeServices.LookupCode>;

namespace Scv.Api.Services
{
    /// <summary>
    /// This should handle caching and LookupServicesClient.
    /// </summary>
    public class LookupService
    {
        #region Variables

        private readonly IAppCache _cache;
        private readonly IConfiguration _configuration;
        private readonly LookupCodeServicesClient _lookupClient;

        #endregion Variables

        #region Properties

        #endregion Properties

        #region Constructor

        public LookupService(IConfiguration configuration, LookupCodeServicesClient lookupClient, IAppCache cache)
        {
            _configuration = configuration;
            _lookupClient = lookupClient;
            _cache = cache;
            _cache.DefaultCachePolicy.DefaultCacheDurationSeconds = int.Parse(configuration.GetNonEmptyValue("Caching:LookupExpiryMinutes")) * 60;
            SetupLookupServicesClient();
        }

        #endregion Constructor

        #region Collection Methods

        private async Task<CodeLookup> GetAgencyLocations()
        {
            return await GetDataFromCache("AgencyLocations",
                async () => await _lookupClient.CodesAgencylocationsAsync());
        }

        private async Task<CodeLookup> GetCivilAppearanceReasons()
        {
            return await GetDataFromCache("CivilAppearanceReasons",
                async () => await _lookupClient.CodesCivilAppearanceReasonsGetAsync());
        }

        private async Task<CodeLookup> GetCivilAppearanceResults()
        {
            return await GetDataFromCache("CivilAppearanceResults",
                async () => await _lookupClient.CodesCivilAppearanceResultsGetAsync());
        }

        private async Task<CodeLookup> GetCivilAppearanceStatuses()
        {
            return await GetDataFromCache("CivilAppearanceStatuses",
                async () => await _lookupClient.CodesCivilAppearanceStatusesGetAsync());
        }

        private async Task<CodeLookup> GetCivilAssets()
        {
            return await GetDataFromCache("CivilAssetsDescriptions",
                async () => await _lookupClient.CodesCivilAssetsGetAsync());
        }

        private async Task<CodeLookup> GetCivilCounselAttendanceTypes()
        {
            return await GetDataFromCache("CivilCounselAttendanceTypes",
                async () => await _lookupClient.CodesCivilPastappearancePartyappearancemethodCounselGetAsync());
        }
        private async Task<CodeLookup> GetCivilDocumentIssueTypes()
        {
            return await GetDataFromCache("GetCivilDocumentIssueTypes",
                async () => await _lookupClient.CodesCivilDocumentIssuetypesGetAsync());
        }

        private async Task<CodeLookup> GetCivilDocumentIssueResults()
        {
            return await GetDataFromCache("GetCivilDocumentIssueResults",
                async () => await _lookupClient.CodesCivilDocumentIssueresultsGetAsync());
        }

        private async Task<CodeLookup> GetCivilPartyAttendanceTypes()
        {
            return await GetDataFromCache("CivilPartyAttendanceTypes",
                async () => await _lookupClient.CodesCivilPastappearancePartyappearancemethodPartyattendancetypesGetAsync());
        }

        private async Task<CodeLookup> GetComplexityTypeDescription()
        {
            return await GetDataFromCache("ComplexityTypes",
                async () => await _lookupClient.CodesFileComplexitiesGetAsync());
        }

        private async Task<CodeLookup> GetCriminalAccusedAttends()
        {
            return await GetDataFromCache("CriminalAccusedAttends",
                async () => await _lookupClient.CodesCriminalPastappearancePartyappearancemethodAccusedcounselGetAsync());
        }

        private async Task<CodeLookup> GetCriminalAdjudicatorAttends()
        {
            return await GetDataFromCache("CriminalAdjudicatorAttends",
                async () => await _lookupClient.CodesCriminalPastappearancePartyappearancemethodAdjudicatorGetAsync());
        }

        private async Task<CodeLookup> GetCriminalCounselAttends()
        {
            return await GetDataFromCache("CriminalCounselAttends",
                async () => await _lookupClient.CodesCriminalPastappearancePartyappearancemethodAccusedcounselGetAsync());
        }

        private async Task<CodeLookup> GetCriminalCrownAttends()
        {
            return await GetDataFromCache("CriminalCrownAttends",
                async () => await _lookupClient.CodesCriminalPastappearancePartyappearancemethodCrownGetAsync());
        }

        private async Task<CodeLookup> GetCriminalAppearanceReasons()
        {
            return await GetDataFromCache("CriminalAppearanceReasons",
                async () => await _lookupClient.CodesCriminalAppearanceReasonsGetAsync());
        }

        private async Task<CodeLookup> GetCriminalAppearanceResults()
        {
            return await GetDataFromCache("CriminalAppearanceResults",
                async () => await _lookupClient.CodesCriminalAppearanceResultsGetAsync());
        }

        private async Task<CodeLookup> GetCriminalAppearanceStatuses()
        {
            return await GetDataFromCache("CriminalAppearanceStatuses",
                async () => await _lookupClient.CodesCriminalAppearanceStatusesGetAsync());
        }

        private async Task<CodeLookup> GetAppearanceDurations()
        {
            return await GetDataFromCache("AppearanceDurations",
                async () => await _lookupClient.CodesCriminalAppearanceDurationGetAsync());
        }

        private async Task<CodeLookup> GetFindings()
        {
            return await GetDataFromCache("Findings",
                async () => await _lookupClient.CodesFindingsGetAsync());
        }

        public async Task<CodeLookup> GetCourtClass()
        {
            var courtClasses = await GetDataFromCache("CourtClasses",
                async () => await _lookupClient.CodesCourtClassesGetAsync());
            return courtClasses;
        }

        private async Task<CodeLookup> GetCourtLevel()
        {
            return await GetDataFromCache("CourtLevels",
                async () => await _lookupClient.CodesCourtLevelsGetAsync());
        }

        private async Task<CodeLookup> GetCriminalAssets()
        {
            return await GetDataFromCache("CriminalAssets",
                async () => await _lookupClient.CodesCriminalAssetsGetAsync());
        }

        private async Task<CodeLookup> GetDocuments()
        {
            return await GetDataFromCache("Documents",
                async () => await _lookupClient.CodesDocumentsGetAsync());
        }

        private async Task<CodeLookup> GetRoles()
        {
            return await GetDataFromCache("Roles",
                async () => await _lookupClient.CodesRolesGetAsync());
        }

        private async Task<CodeLookup> GetParticipantRoles()
        {
            return await GetDataFromCache("ParticipantRoles",
                async () => await _lookupClient.CodesParticipantRolesGetAsync());
        }

        private async Task<CodeLookup> GetWitnessRoles()
        {
            return await GetDataFromCache("WitnessRoles",
                async () => await _lookupClient.CodesWitnessRolesGetAsync());
        }

        private async Task<CodeLookup> GetHearingRestrictions()
        {
            return await GetDataFromCache("HearingRestrictions",
                async () => await _lookupClient.CodesHearingRestrictionsGetAsync());
        }

        private async Task<CodeLookup> GetCriminalSentences()
        {
            return await GetDataFromCache("CriminalSentences",
                async () => await _lookupClient.CodesCriminalSentencesGetAsync());
        }

        #endregion Collection Methods

        #region Lookup Methods

        public async Task<string> GetAgencyLocationDescription(string code) => FindLongDescriptionFromCode(await GetAgencyLocations(), code);

        public async Task<string> GetAgencyLocationCode(string code) => FindShortDescriptionFromCode(await GetAgencyLocations(), code);

        public async Task<string> GetCivilAppearanceStatusDescription(string code) => FindLongDescriptionFromCode(await GetCivilAppearanceStatuses(), code);

        public async Task<string> GetCivilAppearanceReasonsDescription(string code) => FindShortDescriptionFromCode(await GetCivilAppearanceReasons(), code);

        public async Task<string> GetCivilAppearanceResultsDescription(string code) => FindShortDescriptionFromCode(await GetCivilAppearanceResults(), code);

        public async Task<string> GetCivilAssetsDescription(string code) => FindLongDescriptionFromCode(await GetCivilAssets(), code);

        public async Task<string> GetCivilCounselAttendanceType(string code) => FindShortDescriptionFromCode(await GetCivilCounselAttendanceTypes(), code);

        public async Task<string> GetCivilDocumentIssueType(string code) => FindShortDescriptionFromCode(await GetCivilDocumentIssueTypes(), code);

        public async Task<string> GetCivilDocumentIssueResult(string code) => FindShortDescriptionFromCode(await GetCivilDocumentIssueResults(), code);

        public async Task<string> GetCivilPartyAttendanceType(string code) => FindLongDescriptionFromCode(await GetCivilPartyAttendanceTypes(), code);

        public async Task<string> GetComplexityTypeDescription(string code) => FindLongDescriptionFromCode(await GetComplexityTypeDescription(), code);

        public async Task<string> GetCriminalAccusedAttend(string code) => FindShortDescriptionFromCode(await GetCriminalAccusedAttends(), code);

        public async Task<string> GetCriminalAdjudicatorAttend(string code) => FindShortDescriptionFromCode(await GetCriminalAdjudicatorAttends(), code);

        public async Task<string> GetCriminalAssetsDescriptions(string code) => FindLongDescriptionFromCode(await GetCriminalAssets(), code);

        public async Task<string> GetCriminalAppearanceStatusDescription(string code) => FindShortDescriptionFromCode(await GetCriminalAppearanceStatuses(), code);

        public async Task<string> GetCriminalAppearanceReasonsDescription(string code) => FindShortDescriptionFromCode(await GetCriminalAppearanceReasons(), code);

        public async Task<string> GetCriminalAppearanceResultsDescription(string code) => FindLongDescriptionFromCode(await GetCriminalAppearanceResults(), code);

        public async Task<string> GetCriminalCounselAttendanceType(string code) => FindShortDescriptionFromCode(await GetCriminalCounselAttends(), code);

        public async Task<string> GetCriminalCrownAttendanceType(string code) => FindShortDescriptionFromCode(await GetCriminalCrownAttends(), code);

        public async Task<string> GetCriminalSentenceDescription(string code) => FindShortDescriptionFromCode(await GetCriminalSentences(), code);

        public async Task<string> GetAppearanceDuration(string code) => FindLongDescriptionFromCode(await GetAppearanceDurations(), code);

        public async Task<string> GetFindingDescription(string code) => FindLongDescriptionFromCode(await GetFindings(), code);

        public async Task<string> GetActivityClassCdLong(string code) => FindLongDescriptionFromCode(await GetCourtClass(), code);

        public async Task<string> GetActivityClassCdShort(string code) => FindShortDescriptionFromCode(await GetCourtClass(), code);

        public async Task<string> GetCourtClassDescription(string code) => FindShortDescriptionFromCode(await GetCourtClass(), code);

        public async Task<string> GetCourtLevelDescription(string code) => FindShortDescriptionFromCode(await GetCourtLevel(), code);

        public async Task<string> GetCivilRoleTypeDescription(string code) => FindShortDescriptionFromCode(await GetRoles(), code);

        public async Task<string> GetCriminalParticipantRoleDescription(string code) => FindLongDescriptionFromCode(await GetParticipantRoles(), code);

        public async Task<string> GetDocumentDescriptionAsync(string code) => FindShortDescriptionFromCode(await GetDocuments(), code);

        public async Task<string> GetWitnessRoleTypeDescription(string code) => FindShortDescriptionFromCode(await GetWitnessRoles(), code);

        public async Task<string> GetHearingRestrictionDescription(string code) => FindLongDescriptionFromCode(await GetHearingRestrictions(), code);

        /// <summary>
        /// Reads from the configuration for the document category.
        /// </summary>
        /// <param name="documentCode"></param>
        /// <param name="docmClassification"></param>
        /// <returns>string</returns>
        public string GetDocumentCategory(string documentCode, string docmClassification = null)
        {
            var configurationSections =
                _configuration.GetSection("DocumentCategories").Get<Dictionary<string, string>>() ??
                throw new ConfigurationException("Couldn't not build dictionary based on DocumentCategories");

            if (!String.IsNullOrEmpty(documentCode))
            {
                var categoryFromConfig =
                    configurationSections.FirstOrDefault(cs => cs.Value.Split(",").Contains(documentCode)).Key;
                return categoryFromConfig;
            }

            return configurationSections.Keys.Contains(docmClassification, StringComparer.OrdinalIgnoreCase) ? docmClassification : null;
        }

        #endregion Lookup Methods

        #region Helpers

        private async Task<T> GetDataFromCache<T>(string key, Func<Task<T>> fetchFunction)
        {
            return await _cache.GetOrAddAsync(key, async () => await fetchFunction.Invoke());
        }

        private string FindShortDescriptionFromCode(CodeLookup lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.ShortDesc;

        private string FindLongDescriptionFromCode(CodeLookup lookupCodes, string code) => lookupCodes.FirstOrDefault(lookupCode => lookupCode.Code == code)?.LongDesc;

        private void SetupLookupServicesClient()
        {
            _lookupClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
        }

        #endregion Helpers
    }
}
