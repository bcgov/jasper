using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using JCCommon.Clients.FileServices;
using LazyCache;
using MapsterMapper;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Serialization;
using Scv.Api.Helpers.ContractResolver;
using Scv.Api.Helpers.Extensions;
using Scv.Api.Models.Criminal.AppearanceDetail;
using Scv.Api.Models.Criminal.Appearances;
using Scv.Api.Models.Criminal.Detail;
using Scv.Api.Models.Search;
using Scv.Api.Helpers;
using Scv.Db.Models;
using CriminalAppearanceDetail = Scv.Api.Models.Criminal.AppearanceDetail.CriminalAppearanceDetail;
using CriminalAppearanceMethod = Scv.Api.Models.Criminal.AppearanceDetail.CriminalAppearanceMethod;
using CriminalParticipant = Scv.Api.Models.Criminal.Detail.CriminalParticipant;
using CriminalWitness = Scv.Api.Models.Criminal.Detail.CriminalWitness;

namespace Scv.Api.Services.Files
{
    public class CriminalFilesService
    {
        #region Variables

        private readonly IAppCache _cache;
        private readonly FileServicesClient _filesClient;
        private readonly IMapper _mapper;
        private readonly LookupService _lookupService;
        private readonly LocationService _locationService;
        private readonly string _applicationCode;
        private readonly string _requestAgencyIdentifierId;
        private readonly string _requestPartId;
        private readonly ClaimsPrincipal _currentUser;

        #endregion

        #region Constructor

        public CriminalFilesService(IConfiguration configuration, FileServicesClient filesClient, IMapper mapper, LookupService lookupService, LocationService locationService, IAppCache cache,
            ClaimsPrincipal user)
        {
            _filesClient = filesClient;
            _filesClient.JsonSerializerSettings.ContractResolver = new SafeContractResolver { NamingStrategy = new CamelCaseNamingStrategy() };
            _lookupService = lookupService;
            _locationService = locationService;
            _mapper = mapper;
            _applicationCode = user.ApplicationCode();
            _requestAgencyIdentifierId = user.AgencyCode();
            _requestPartId = user.ParticipantId();
            _cache = cache;
            _currentUser = user;
        }

        #endregion Constructor

        #region Methods
        public async Task<FileSearchResponse> SearchAsync(FilesCriminalQuery fcq)
        {
            fcq.FilePermissions =
                "[\"A\", \"Y\", \"T\", \"F\", \"C\", \"M\", \"L\", \"R\", \"B\", \"D\", \"E\", \"G\", \"H\", \"N\", \"O\", \"P\", \"S\", \"V\"]"; // for now, use all types - TODO: determine proper list of types?

            // CourtLevel = "S"  Supreme court data, CourtLevel = "P" - Province.
            // Only Provincial files can be accessed in JASPER
            return await _filesClient.FilesCriminalGetAsync(_requestAgencyIdentifierId,
                _requestPartId, _applicationCode, fcq.SearchMode, fcq.FileHomeAgencyId, fcq.FileNumberTxt,
                fcq.FilePrefixTxt, fcq.FilePermissions, fcq.FileSuffixNo, fcq.MdocRefTypeCode, fcq.CourtClass,
                CourtLevelCd.P, fcq.NameSearchTypeCd, fcq.LastName, fcq.OrgName, fcq.GivenName,
                fcq.Birth?.ToString("yyyy-MM-dd"), fcq.SearchByCrownPartId, fcq.SearchByCrownActiveOnly,
                fcq.SearchByCrownFileDesignation, fcq.MdocJustinNoSet, fcq.PhysicalFileIdSet);
        }

        public async Task<List<RedactedCriminalFileDetailResponse>> GetFilesByAgencyIdCodeAndFileNumberText(string location,
            string fileNumber, CourtLevelCd courtLevel = CourtLevelCd.S)
        {
            var fileDetails = new List<RedactedCriminalFileDetailResponse>();

            var fileNumberText = fileNumber.Contains("-") ? fileNumber.Split("-")[0] : fileNumber;
            var mdocSequenceNumber = fileNumber.Contains("-") ? fileNumber.Split("-")[1] : null;
            var fileSearchResponse = await SearchAsync(new FilesCriminalQuery
            {
                FileHomeAgencyId = location,
                FileNumberTxt = fileNumberText,
                SearchMode = SearchMode.FILENO,
                CourtLevel = courtLevel
            });

            var fileIdAndAppearanceDate = fileSearchResponse?.FileDetail?.Where(fd => mdocSequenceNumber == null || fd.MdocSeqNo == mdocSequenceNumber)
                .SelectToList(fd => new { fd.MdocJustinNo, fd.NextApprDt });

            if (fileIdAndAppearanceDate == null || fileIdAndAppearanceDate.Count == 0)
                return fileDetails;

            //Return the basic entry without doing a lookup.
            if (fileIdAndAppearanceDate.Count == 1)
                return new List<RedactedCriminalFileDetailResponse> { new RedactedCriminalFileDetailResponse { JustinNo = fileIdAndAppearanceDate.First().MdocJustinNo } };

            //It seems the fileSearch and the FileDetails/FileContent bring up two different participant lists
            //The fileSearch seems to include have extra participants.
            var fileDetailTasks = new List<Task<CriminalFileDetailResponse>>();
            foreach (var fileIdAndAppearance in fileIdAndAppearanceDate)
            {
                async Task<CriminalFileDetailResponse> FileDetails() =>
                    await _filesClient.FilesCriminalGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode,
                        fileIdAndAppearance.MdocJustinNo);
                fileDetailTasks.Add(_cache.GetOrAddAsync($"CriminalFileDetail-{fileIdAndAppearance.MdocJustinNo}-{_requestAgencyIdentifierId}", FileDetails));
            }

            var fileDetailResponses = await fileDetailTasks.WhenAll();
            fileDetails = fileDetailResponses.SelectToList(fdr => _mapper.Map<RedactedCriminalFileDetailResponse>(fdr));

            foreach (var fileDetail in fileDetails)
                fileDetail.NextApprDt = fileIdAndAppearanceDate.First(fa => fa.MdocJustinNo == fileDetail.JustinNo)
                    .NextApprDt;

            return fileDetails;
        }

        public async Task<RedactedCriminalFileDetailResponse> FileIdAsync(string fileId)
        {
            async Task<CriminalFileDetailResponse> FileDetails() => await _filesClient.FilesCriminalGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId);
            async Task<CriminalFileContent> FileContent() => await _filesClient.FilesCriminalFilecontentAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, null, null, null, null, fileId);
            async Task<CriminalFileAppearances> FileAppearances() => await PopulateDetailsAppearancesAsync(fileId, FutureYN.Y, HistoryYN.Y);
            async Task<CourtList> CourtList(string agencyId, string courtRoomCd, string appearanceDt, string fileNumber) => await _filesClient.FilesCourtlistAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, agencyId, courtRoomCd, appearanceDt, "CR", fileNumber);

            var fileDetailTask = _cache.GetOrAddAsync($"CriminalFileDetail-{fileId}-{_requestAgencyIdentifierId}", FileDetails);
            var fileContentTask = _cache.GetOrAddAsync($"CriminalFileContent-{fileId}-{_requestAgencyIdentifierId}", FileContent);
            var appearancesTask = _cache.GetOrAddAsync($"CriminalAppearancesFull-{fileId}-{_requestAgencyIdentifierId}", FileAppearances);

            var fileDetail = await fileDetailTask;
            var fileContent = await fileContentTask;
            var appearances = await appearancesTask;

            IEnumerable<ClCriminalCourtList> criminalCourtLists = default;

            if (appearances != null && appearances.ApprDetail.Count != 0)
            {
                var latestAppearance = appearances.ApprDetail.OrderByDescending(a => a.AppearanceDt).FirstOrDefault();
                if (latestAppearance != null)
                {
                    var agencyId = await _locationService.GetLocationAgencyIdentifier(fileDetail.HomeLocationAgenId);
                    var courtList = await _cache.GetOrAddAsync($"CriminalCourtList-{agencyId}-{latestAppearance.CourtRoomCd}-{latestAppearance.AppearanceDt}-{fileDetail.FileNumberTxt}-{_requestAgencyIdentifierId}",
                        () => CourtList(agencyId, latestAppearance.CourtRoomCd, latestAppearance.AppearanceDt, fileDetail.FileNumberTxt));
                    criminalCourtLists = courtList.CriminalCourtList;
                }
            }

            //CriminalFileContent can return null when an invalid fileId is inserted.
            if (fileDetail == null || fileContent == null)
                return null;

            var detail = _mapper.Map<RedactedCriminalFileDetailResponse>(fileDetail);
            var documents = PopulateDetailDocuments(fileContent);
            detail = await PopulateBaseDetail(detail);
            detail.Appearances = appearances;
            detail.Witness = await PopulateDetailWitnesses(detail);
            detail.Participant = await PopulateDetailParticipants(detail, documents, fileContent.AccusedFile, criminalCourtLists);
            detail.HearingRestriction = await PopulateDetailHearingRestrictions(detail);
            detail.Crown = PopulateDetailCrown(detail);
            return detail;
        }

        public async Task<CriminalAppearanceDetail> AppearanceDetailAsync(string fileId, string appearanceId, string partId)
        {
            async Task<CriminalFileDetailResponse> FileDetails() => await _filesClient.FilesCriminalGetAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, fileId);
            async Task<CriminalFileContent> FileContent() => await _filesClient.FilesCriminalFilecontentAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, null, null, null, null, fileId);
            async Task<CriminalFileAppearances> Appearances() => await PopulateDetailsAppearancesAsync(fileId, FutureYN.Y, HistoryYN.Y);
            async Task<CriminalFileAppearanceCountResponse> AppearanceCounts() => await _filesClient.FilesCriminalAppearanceCountsAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, appearanceId);
            async Task<CriminalFileAppearanceApprMethodResponse> AppearanceMethods() => await _filesClient.FilesCriminalAppearanceAppearancemethodsAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, appearanceId);

            var appearancesTask = _cache.GetOrAddAsync($"CriminalAppearancesFull-{fileId}-{_requestAgencyIdentifierId}", Appearances);
            var fileDetailTask = _cache.GetOrAddAsync($"CriminalFileDetail-{fileId}-{_requestAgencyIdentifierId}", FileDetails);
            var fileContentTask = _cache.GetOrAddAsync($"CriminalFileContent-{fileId}-{_requestAgencyIdentifierId}", FileContent);
            var appearanceCountTask = _cache.GetOrAddAsync($"CriminalAppearanceCounts-{fileId}-{appearanceId}-{_requestAgencyIdentifierId}", AppearanceCounts);
            var appearanceMethodsTask = _cache.GetOrAddAsync($"CriminalAppearanceMethods-{fileId}-{appearanceId}-{_requestAgencyIdentifierId}", AppearanceMethods);

            //Probably cached, user has the path: FileDetails -> Appearances -> AppearanceDetails. 
            var appearances = await appearancesTask;
            var detail = _mapper.Map<RedactedCriminalFileDetailResponse>(await fileDetailTask);
            var agencyId = await _locationService.GetLocationAgencyIdentifier(detail.HomeLocationAgenId);
            detail.Witness = await PopulateDetailWitnesses(detail);

            var targetAppearance = appearances?.ApprDetail?.FirstOrDefault(app => app.AppearanceId == appearanceId && app.PartId == partId);
            if (targetAppearance == null || agencyId == null)
                return null;

            async Task<CourtList> CourtList() => await _filesClient.FilesCourtlistAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, agencyId, targetAppearance.CourtRoomCd, targetAppearance.AppearanceDt, "CR", detail.FileNumberTxt);
            var courtList = await _cache.GetOrAddAsync($"CriminalCourtList-{agencyId}-{targetAppearance.CourtRoomCd}-{targetAppearance.AppearanceDt}-{detail.FileNumberTxt}-{_requestAgencyIdentifierId}", CourtList);
            var fileContent = await fileContentTask;
            var appearanceCount = await appearanceCountTask;
            var appearanceMethods = await appearanceMethodsTask;

            var accusedFile = fileContent?.AccusedFile.FirstOrDefault(af => af.MdocJustinNo == fileId && af.PartId == partId);
            var criminalParticipant = detail?.Participant.FirstOrDefault(x => x.PartId == partId);
            var appearanceFromAccused = accusedFile?.Appearance.FirstOrDefault(a => a?.AppearanceId == appearanceId);
            var targetCourtList = courtList.CriminalCourtList.FirstOrDefault(cl => cl.CriminalAppearanceID == appearanceId);
            var attendanceMethods = targetCourtList?.AttendanceMethod;

            //CourtList AttendanceMethod is present state, AppearanceMethod is past state they look to be the same values, but haven't found any test data on DEV where they differ. 
            if (criminalParticipant == null || accusedFile == null || appearanceFromAccused == null)
                return null;

            var appearanceDetail = new CriminalAppearanceDetail
            {
                JustinNo = fileId,
                AgencyId = await _locationService.GetLocationAgencyIdentifier(detail.HomeLocationAgenId),
                AppearanceId = appearanceId,
                PartId = targetAppearance.PartId,
                ProfSeqNo = targetAppearance.ProfSeqNo,
                CourtRoomCd = targetAppearance.CourtRoomCd,
                FileNumberTxt = detail.FileNumberTxt,
                AppearanceMethods = await PopulateAppearanceMethods(appearanceMethods.AppearanceMethod),
                AppearanceDt = targetAppearance.AppearanceDt,
                //AppearanceNote = appearanceFromAccused.AppearanceNote?.ReturnNullIfEmpty(),
                //JudgesRecommendation = appearanceFromAccused.JudgesRecommendation,
                EstimatedTimeHour = appearanceFromAccused.EstimatedTimeHour?.ReturnNullIfEmpty(),
                EstimatedTimeMin = appearanceFromAccused.EstimatedTimeMin?.ReturnNullIfEmpty(),
                Accused = await PopulateAppearanceCriminalAccused(criminalParticipant.FullName, appearanceFromAccused, attendanceMethods, partId, appearanceMethods.AppearanceMethod),
                Prosecutor = await PopulateAppearanceDetailProsecutor(appearanceFromAccused, attendanceMethods, appearanceMethods.AppearanceMethod),
                Adjudicator = await PopulateAppearanceDetailAdjudicator(appearanceFromAccused, attendanceMethods, appearanceMethods.AppearanceMethod),
                JustinCounsel = await PopulateAppearanceDetailJustinCounsel(criminalParticipant, appearanceFromAccused, attendanceMethods, appearanceMethods.AppearanceMethod),
                Charges = await PopulateCharges(appearanceCount.ApprCount),
                InitiatingDocuments = GetInitiatingDocuments(accusedFile.Document),
                CourtLevelCd = detail.CourtLevelCd
            };
            return appearanceDetail;
        }

        #endregion

        #region Helpers

        #region Criminal Details

        private List<CriminalDocument> GetInitiatingDocuments(ICollection<CfcDocument> documents)
        {
            return documents?.Where(doc => doc?.DocmClassification == "Initiating" && !string.IsNullOrEmpty(doc.ImageId))
                .Select(a => new CriminalDocument { IssueDate = a.IssueDate, ImageId = a.ImageId }).ToList();
        }

        private async Task<CriminalFileAppearances> PopulateDetailsAppearancesAsync(string fileId, FutureYN? future, HistoryYN? history)
        {
            var criminalFileIdAppearances = await _filesClient.FilesCriminalAppearancesAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, future, history, fileId);
            var criminalAppearances = _mapper.Map<CriminalFileAppearances>(criminalFileIdAppearances);
            foreach (var appearance in criminalAppearances.ApprDetail)
            {
                appearance.AppearanceReasonDsc = await _lookupService.GetCriminalAppearanceReasonsDescription(appearance.AppearanceReasonCd);
                appearance.AppearanceResultDsc = await _lookupService.GetCriminalAppearanceResultsDescription(appearance.AppearanceResultCd);
                appearance.AppearanceStatusDsc = await _lookupService.GetCriminalAppearanceStatusDescription(appearance.AppearanceStatusCd.ToString());
                appearance.CourtLocationId = await _locationService.GetLocationAgencyIdentifier(appearance.CourtAgencyId);
                appearance.CourtLocation = await _locationService.GetLocationName(appearance.CourtAgencyId);
            }
            return criminalAppearances;
        }

        private string GetParticipantIdFromDetail(string partId, RedactedCriminalFileDetailResponse detail) => detail.Participant?.FirstOrDefault(p => p != null && p.PartId == partId)?.PartId;

        private List<CriminalBan> PopulateBans(CfcAccusedFile accusedFile)
        {
            var bans = _mapper.Map<List<CriminalBan>>(accusedFile.Ban.Where(b => b != null));
            bans.ForEach(b => b.PartId = accusedFile.PartId);
            return bans;
        }

        private async Task<List<CriminalCount>> PopulateCounts(CfcAccusedFile accusedFile, RedactedCriminalFileDetailResponse detail)
        {
            var criminalCount = new List<CriminalCount>();
            foreach (var appearance in accusedFile.Appearance.Where(a => a != null))
            {
                foreach (var count in _mapper.Map<ICollection<CriminalCount>>(appearance.AppearanceCount.Where(a => a?.AppearanceResult == "END")))
                {
                    count.PartId = GetParticipantIdFromDetail(accusedFile.PartId, detail);
                    count.AppearanceDate = appearance.AppearanceDate;
                    count.Sentence = count.Sentence.Where(s => s != null).ToList();
                    foreach (var criminalSentence in count.Sentence)
                    {
                        criminalSentence.JudgesRecommendation = appearance.JudgesRecommendation;
                        criminalSentence.SentenceTypeDesc = await _lookupService.GetCriminalSentenceDescription(criminalSentence.SntpCd);
                    }
                    count.FindingDsc = await _lookupService.GetFindingDescription(count.Finding);
                    criminalCount.Add(count);
                }
            }
            return criminalCount;
        }

        private List<CriminalDocument> PopulateDetailDocuments(CriminalFileContent criminalFileContent)
        {
            return criminalFileContent.AccusedFile.SelectMany(ac =>
            {
                var criminalDocuments = _mapper.Map<List<CriminalDocument>>(ac.Document);

                //Create ROPs.
                if (ac.Appearance != null && ac.Appearance.Any())
                {
                    criminalDocuments.Insert(0, new CriminalDocument
                    {
                        DocumentTypeDescription = "Record of Proceedings",
                        ImageId = ac.PartId,
                        Category = "rop",
                        PartId = ac.PartId,
                        HasFutureAppearance = ac.Appearance?.Any(a =>
                            a?.AppearanceDate != null && DateTime.Parse(a.AppearanceDate) >= DateTime.Today)
                    });
                }

                //Populate extra fields.
                foreach (var document in criminalDocuments)
                {
                    document.Category = string.IsNullOrEmpty(document.Category) ? _lookupService.GetDocumentCategory(document.DocmFormId, document.DocmClassification) : document.Category;
                    document.DocumentTypeDescription = document.DocmFormDsc;
                    document.PartId = string.IsNullOrEmpty(ac.PartId) ? null : ac.PartId;
                    document.DocmId = string.IsNullOrEmpty(document.DocmId) ? null : document.DocmId;
                    document.ImageId = string.IsNullOrEmpty(document.ImageId) ? null : document.ImageId;
                    document.HasFutureAppearance = ac.Appearance?.Any(a =>
                        a?.AppearanceDate != null && DateTime.Parse(a.AppearanceDate) >= DateTime.Today);
                }
                return criminalDocuments;
            }).ToList();
        }

        private async Task<ICollection<CriminalWitness>> PopulateDetailWitnesses(RedactedCriminalFileDetailResponse detail)
        {
            foreach (var witness in detail.Witness)
            {
                witness.AgencyCd = await _lookupService.GetAgencyLocationCode(witness.AgencyId);
                witness.AgencyDsc = await _lookupService.GetAgencyLocationDescription(witness.AgencyId);
                witness.WitnessTypeDsc = await _lookupService.GetWitnessRoleTypeDescription(witness.WitnessTypeCd);
            }
            return detail.Witness;
        }

        private async Task<ICollection<CriminalParticipant>> PopulateDetailParticipants(RedactedCriminalFileDetailResponse detail, ICollection<CriminalDocument> documents, ICollection<CfcAccusedFile> accusedFiles, IEnumerable<ClCriminalCourtList> courtLists)
        {
            foreach (var participant in detail.Participant)
            {
                participant.AgeNotice = courtLists?.Where(cl => cl.FileInformation.PartId == participant.PartId)
                    .SelectMany(cl => cl.AgeNotice)
                    .ToList();
                participant.Document = documents.Where(doc => doc.PartId == participant.PartId).ToList();
                participant.HideJustinCounsel = false;   //TODO tie this to a permission. View Witness List permission
                //TODO COUNSEL? This would have to come from law society data, which is stored in a CSV file. 
                foreach (var accusedFile in accusedFiles.Where(af => af?.PartId == participant.PartId))
                {
                    participant.Count.AddRange(await PopulateCounts(accusedFile, detail));
                    participant.Ban.AddRange(PopulateBans(accusedFile));
                }
            }
            return detail.Participant;
        }

        private async Task<RedactedCriminalFileDetailResponse> PopulateBaseDetail(RedactedCriminalFileDetailResponse detail)
        {
            detail.HomeLocationAgencyName = await _locationService.GetLocationName(detail.HomeLocationAgenId);
            detail.HomeLocationAgencyCode = await _locationService.GetLocationAgencyIdentifier(detail.HomeLocationAgenId);
            detail.HomeLocationRegionName = await _locationService.GetRegionName(detail.HomeLocationAgencyCode);
            detail.CourtClassDescription = await _lookupService.GetCourtClassDescription(detail.CourtClassCd.ToString());
            detail.CourtLevelDescription = await _lookupService.GetCourtLevelDescription(detail.CourtLevelCd.ToString());
            detail.ActivityClassCd = await _lookupService.GetActivityClassCdLong(detail.CourtClassCd.ToString());
            detail.ActivityClassDesc = await _lookupService.GetActivityClassCdShort(detail.CourtClassCd.ToString());
            //Some lookups have LongDesc and ShortDesc the same. 
            if (detail.ActivityClassCd == detail.ActivityClassDesc)
                detail.ActivityClassCd = detail.CourtClassCd.ToString();
            detail.CrownEstimateLenDsc = detail.CrownEstimateLenUnit.HasValue ? await _lookupService.GetAppearanceDuration(detail.CrownEstimateLenUnit.Value.ToString()) : null;
            detail.AssignmentTypeDsc = await _lookupService.GetComplexityTypeDescription(detail.ComplexityTypeCd?.ToString());
            return detail;
        }

        private async Task<ICollection<CriminalHearingRestriction>> PopulateDetailHearingRestrictions(RedactedCriminalFileDetailResponse detail)
        {
            // Assigned restriction is only included if current user is assigned to any of the role
            var includeAssigned = _currentUser
                .HasRoles(
                [
                    Role.ADMIN,
                    Role.ACJ_CHIEF_JUDGE,
                    Role.RAJ,
                    Role.PO_MANAGER
                ], true);
            var restrictionTypeOrder = new List<HearingRestriction2HearingRestrictionTypeCd>
            {
                HearingRestriction2HearingRestrictionTypeCd.S,
                HearingRestriction2HearingRestrictionTypeCd.G,
                HearingRestriction2HearingRestrictionTypeCd.A,
                HearingRestriction2HearingRestrictionTypeCd.D,
            };
            var restrictions = detail.HearingRestriction
                .Where(r => includeAssigned
                    || r.HearingRestrictionTypeCd != HearingRestriction2HearingRestrictionTypeCd.G)
                .OrderBy(r => restrictionTypeOrder.IndexOf(r.HearingRestrictionTypeCd))
                .ThenBy(r => r.AdjFullNm);

            foreach (var hearingRestriction in restrictions)
            {
                hearingRestriction.HearingRestrictionTypeDsc = await _lookupService.GetHearingRestrictionDescription(hearingRestriction.HearingRestrictionTypeCd.ToString());
            }

            detail.HearingRestriction = [.. restrictions];

            return detail.HearingRestriction;
        }

        private ICollection<CrownWitness> PopulateDetailCrown(RedactedCriminalFileDetailResponse detail)
        {
            var crownWitnesses = _mapper.Map<ICollection<CrownWitness>>(detail.Witness.Where(w => w.RoleTypeCd == CriminalWitnessRoleTypeCd.CRN).ToList());
            foreach (var crownWitness in crownWitnesses)
            {
                crownWitness.Assigned = crownWitness.IsAssigned(detail.AssignedPartNm);
            }
            return crownWitnesses;
        }

        #endregion Criminal Details

        #region Criminal Appearance Details

        private async Task<CriminalAccused> PopulateAppearanceCriminalAccused(string fullName, CfcAppearance appearanceFromAccused, ICollection<ClAttendanceMethod> attendanceMethods, string partId, ICollection<JCCommon.Clients.FileServices.CriminalAppearanceMethod> appearanceMethods)
        {
            var partyAppearanceMethod = appearanceFromAccused?.PartyAppearanceMethod.FirstOrDefault(pam => pam.PartyRole == "ACC");
            var attendanceMethod = attendanceMethods?.FirstOrDefault(am => am.RoleType == "ACC");
            var appearanceMethod = appearanceMethods?.FirstOrDefault(am => am.RoleTypeCd == "ACC");
            return new CriminalAccused
            {
                FullName = fullName,
                PartId = partId, //partyAppearanceMethod, doesn't always have a partId on DEV at least.
                PartyAppearanceMethod = partyAppearanceMethod?.PartyAppearanceMethod,
                PartyAppearanceMethodDesc = await _lookupService.GetCriminalAccusedAttend(partyAppearanceMethod?.PartyAppearanceMethod),
                AttendanceMethodCd = attendanceMethod?.AttendanceMethodCd,
                AttendanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(attendanceMethod?.AttendanceMethodCd),
                AppearanceMethodCd = appearanceMethod?.AppearanceMethodCd,
                AppearanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(appearanceMethod?.AppearanceMethodCd)
            };
        }

        private async Task<CriminalAdjudicator> PopulateAppearanceDetailAdjudicator(CfcAppearance appearanceFromAccused, ICollection<ClAttendanceMethod> attendanceMethods, ICollection<JCCommon.Clients.FileServices.CriminalAppearanceMethod> appearanceMethods)
        {
            var partyAppearanceMethod = appearanceFromAccused?.PartyAppearanceMethod.FirstOrDefault(pam => pam.PartyRole == "ADJ");
            var attendanceMethod = attendanceMethods?.FirstOrDefault(am => am.RoleType == "ADJ");
            var appearanceMethod = appearanceMethods?.FirstOrDefault(am => am.RoleTypeCd == "ADJ");
            if (partyAppearanceMethod == null || partyAppearanceMethod.PartyName == null && partyAppearanceMethod.PartId == null && appearanceMethod?.AppearanceMethodCd == null)
                return null;

            return new CriminalAdjudicator
            {
                FullName = partyAppearanceMethod.PartyName.ConvertNameLastCommaFirstToFirstLast(),
                PartId = partyAppearanceMethod.PartId,
                PartyAppearanceMethod = partyAppearanceMethod.PartyAppearanceMethod,
                PartyAppearanceMethodDesc = await _lookupService.GetCriminalAdjudicatorAttend(partyAppearanceMethod?.PartyAppearanceMethod),
                AttendanceMethodCd = attendanceMethod?.AttendanceMethodCd,
                AttendanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(attendanceMethod?.AttendanceMethodCd),
                AppearanceMethodCd = appearanceMethod?.AppearanceMethodCd,
                AppearanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(appearanceMethod?.AppearanceMethodCd)
            };
        }

        private async Task<Prosecutor> PopulateAppearanceDetailProsecutor(CfcAppearance appearanceFromAccused, ICollection<ClAttendanceMethod> attendanceMethods, ICollection<JCCommon.Clients.FileServices.CriminalAppearanceMethod> appearanceMethods)
        {
            var partyAppearanceMethod = appearanceFromAccused?.PartyAppearanceMethod.FirstOrDefault(pam => pam.PartyRole == "PRO");
            var attendanceMethod = attendanceMethods?.FirstOrDefault(am => am.RoleType == "PRO");
            var appearanceMethod = appearanceMethods?.FirstOrDefault(am => am.RoleTypeCd == "PRO");
            if (partyAppearanceMethod == null || partyAppearanceMethod.PartyName == null && partyAppearanceMethod.PartId == null && appearanceMethod?.AppearanceMethodCd == null)
                return null;

            return new Prosecutor
            {
                FullName = partyAppearanceMethod.PartyName.ConvertNameLastCommaFirstToFirstLast(),
                PartId = partyAppearanceMethod.PartId,
                PartyAppearanceMethod = partyAppearanceMethod.PartyAppearanceMethod,
                PartyAppearanceMethodDesc = await _lookupService.GetCriminalCrownAttendanceType(partyAppearanceMethod?.PartyAppearanceMethod),
                AttendanceMethodCd = attendanceMethod?.AttendanceMethodCd,
                AttendanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(attendanceMethod?.AttendanceMethodCd),
                AppearanceMethodCd = appearanceMethod?.AppearanceMethodCd,
                AppearanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(appearanceMethod?.AppearanceMethodCd)
            };
        }

        private async Task<JustinCounsel> PopulateAppearanceDetailJustinCounsel(CriminalParticipant criminalParticipant, CfcAppearance appearanceFromAccused, ICollection<ClAttendanceMethod> attendanceMethods, ICollection<JCCommon.Clients.FileServices.CriminalAppearanceMethod> appearanceMethods)
        {
            if (criminalParticipant == null)
                return null;

            var justinCounsel = _mapper.Map<JustinCounsel>(criminalParticipant);
            var partyAppearanceMethod = appearanceFromAccused?.PartyAppearanceMethod.FirstOrDefault(pam => pam.PartyRole == "CON");
            var attendanceMethod = attendanceMethods?.FirstOrDefault(am => am.RoleType == "CON");
            var appearanceMethod = appearanceMethods?.FirstOrDefault(am => am.RoleTypeCd == "CON");
            justinCounsel.PartyAppearanceMethod = partyAppearanceMethod?.PartyAppearanceMethod;
            justinCounsel.PartyAppearanceMethodDesc = await _lookupService.GetCriminalCounselAttendanceType(partyAppearanceMethod?.PartyAppearanceMethod);
            justinCounsel.AttendanceMethodCd = attendanceMethod?.AttendanceMethodCd;
            justinCounsel.AttendanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(attendanceMethod?.AttendanceMethodCd);
            justinCounsel.AppearanceMethodCd = appearanceMethod?.AppearanceMethodCd;
            justinCounsel.AppearanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(appearanceMethod?.AppearanceMethodCd);
            //We could assign name here from the PartyAppearance, but that doesn't appear to be correct. 
            //While testing I found data inside of PartyAppearances, but used CourtList and saw there was no assigned counsel. 
            //FileId 1180, AppearanceId 1528.0026, PartId 15911.0026 is an example of this on the Development Environment. 
            return justinCounsel;
        }

        public async Task<CriminalFileContent> FileContentAsync(string agencyId, string roomCode, DateTime? proceeding, string appearanceId, string justinNumber)
        {
            var proceedingDateString = proceeding.HasValue ? proceeding.Value.ToString("yyyy-MM-dd") : "";
            return await _filesClient.FilesCriminalFilecontentAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, agencyId, roomCode,
                proceedingDateString, appearanceId, justinNumber);
        }

        public async Task<RopResponse> RecordOfProceedingsAsync(string partId, string profSequenceNumber, CourtLevelCd courtLevelCode, CourtClassCd courtClassCode)
        {
            return await _filesClient.FilesRecordOfProceedingsAsync(_requestAgencyIdentifierId, _requestPartId, _applicationCode, partId, profSequenceNumber, courtLevelCode, courtClassCode);
        }

        private async Task<ICollection<CriminalCharges>> PopulateCharges(ICollection<CriminalAppearanceCount> appearanceCount)
        {
            var charges = _mapper.Map<ICollection<CriminalCharges>>(appearanceCount);
            //Populate charges or counts extra fields.
            foreach (var charge in charges)
            {
                charge.AppearanceReasonDsc = await _lookupService.GetCriminalAppearanceReasonsDescription(charge.AppearanceReasonCd);
                charge.AppearanceResultDesc = await _lookupService.GetCriminalAppearanceResultsDescription(charge.AppearanceResultCd);
                charge.FindingDsc = await _lookupService.GetFindingDescription(charge.FindingCd);
            }
            return charges;
        }

        private async Task<ICollection<CriminalAppearanceMethod>> PopulateAppearanceMethods(ICollection<JCCommon.Clients.FileServices.CriminalAppearanceMethod> appearanceMethods)
        {
            var criminalAppearanceMethods = _mapper.Map<ICollection<CriminalAppearanceMethod>>(appearanceMethods);
            //Populate appearance methods extra fields.
            foreach (var appearanceMethod in criminalAppearanceMethods)
            {
                appearanceMethod.AppearanceMethodDesc = await _lookupService.GetCriminalAssetsDescriptions(appearanceMethod.AppearanceMethodCd);
                appearanceMethod.RoleTypeDsc = await _lookupService.GetCriminalParticipantRoleDescription(appearanceMethod.RoleTypeCd);
            }
            return criminalAppearanceMethods;
        }

        #endregion Criminal Appearance Details

        #endregion
    }
}
