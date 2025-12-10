import { AdditionalProperties } from '../../common';

export interface courtListType {
  criminalCourtList: criminalCourtListType[];
  civilCourtList: civilCourtListType[];
  courtLocationName: string;
  courtRoomCode: string;
  courtProceedingsDate: string;
  fileSearchParameter: civilFileSearchParameterType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilFileSearchParameterType {
  courtDivisionCd: string;
  fileNumber: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilOrderToVaryType {
  documentId: string;
  documentTypeDsc: string;
  adjudicatorName: string;
  dateGranted: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilUnscheduledDocumentType {
  documentId: string;
  fileSeqNumber: string;
  documentTypeDsc: string;
  filedBy: civilFiledByType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface arrestWarrantType {
  fileNumberText: string;
  warrantDate: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface protectedPartyNameType {
  protectedPartyName: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface restrainingPartyNameType {
  restrainingPartyName: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface protectionOrderType {
  orderTypeDsc: string;
  restrainingPartyName: restrainingPartyNameType[];
  protectedPartyName: protectedPartyNameType[];
  pororderIssueDate: string;
  porconditionText: string;
  pororderExpiryDate: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface speakerEventType {
  speakerEventDate: string;
  speakerEventTime: string;
  speakerEventText: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface speakerType {
  speakerId: string;
  speakerTypeCd: string;
  speakerSeqNo: string;
  voirDireSeqNo: string;
  speakerName: string;
  speakerStatusCd: string;
  speakerStatusDsc: string;
  speakerEvent: speakerEventType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilPartyNameType {
  nameTypeCd: string;
  nameTypeDsc: string;
  surnameNm: string;
  firstGivenNm: string;
  secondGivenNm: string;
  thirdGivenNm: string;
  organizationNm: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilPartyRoleType {
  roleTypeCd: string;
  roleTypeDsc: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilLegalRepresentativeType {
  legalRepTypeDsc: string;
  legalRepFullName: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilRepresentativeType {
  repFullName: string;
  attendanceMethodCd: string;
  phoneNumber: string;
  instruction: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilCounselType {
  counselFullName: string;
  counselId: string;
  phoneNumber: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilPartiesType {
  partyId: string;
  partyScheduled: string;
  partyRoleType: string;
  partyFullName: string;
  attendanceMethodCd: string;
  partyTypeCd: string;
  leftRightParty: string;
  birthDate: string;
  phoneNumber: string;
  instruction: string;
  partyFullAddressText: string;
  warrantIssueDate: string;
  activeYN: string;
  counsel: civilCounselType[];
  representative: civilRepresentativeType[];
  legalRepresentative: civilLegalRepresentativeType[];
  partyRole: civilPartyRoleType[];
  partyName: civilPartyNameType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilAssetType {
  assetTypeDescription: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilPhysicalFileType {
  physicalFileID: string;
  fileAccessLevelCd: string;
  fileNumber: string;
  styleOfCause: string;
  leftPartyLastName: string;
  leftPartyGivenName: string;
  leftPartyOtherCount: string;
  rightPartyLastName: string;
  rightPartyGivenName: string;
  rightPartyOtherCount: string;
  thirdPartyLastName: string;
  thirdPartyGivenName: string;
  thirdPartyOtherCount: string;
  homeAgencyCd: string;
  civilAgencyCd: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilFiledByType {
  filedByName: string;
  roleTypeCode: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilIssueType {
  issueDescription: string;
  issueNumber: string;
  issueType: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilDocumentType {
  category: string;
  appearanceID: string;
  documentId: string;
  fileSeqNumber: string;
  roleTypeCode: string;
  documentTypeDescription: string;
  estimatedDocumentMinutes: string;
  documentTypeCd: string;
  dateGranted: string;
  dateVaried: string;
  cancelledDate: string;
  adjudicatorPartId: string;
  adjudicatorName: string;
  orderDocumentYN: string;
  documentAccessLevelCd: string;
  documentSealStartDate: string;
  documentSealEndDate: string;
  appearanceReasonCode: string;
  issue: civilIssueType[];
  filedBy: civilFiledByType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface hearingRestrictionType {
  hearingRestrictionTypeDesc: string;
  adjInitialsText: string;
  hearingRestrictiontype: string;
  judgeName: string;
  hearingRestrictionDate: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface scheduledAppearanceType {
  appearanceId: string;
  courtAgencyIdentifier: string;
  courtRoom: string;
  appearanceDate: string;
  appearanceTime: string;
  appearanceReasonCd: string;
  estDurationHours: string;
  estDurationMins: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilCourtListType {
  estimatedTimeHour: string;
  estimatedTimeMin: string;
  cfcsaFile: boolean;
  video: boolean;
  remoteVideo: boolean;
  activityClassCd: string;
  activityClassDesc: string;
  outOfTownJudge: string;
  supplementalEquipment: string;
  securityRestriction: string;
  appearanceReasonDesc: string;
  appearanceReasonCd: string;
  appearanceStatusCd: string;
  judgeInitials: string;
  commentToJudgeText: string;
  fileCommentText: string;
  trialRemarkTxt: string;
  scheduledAppearance: scheduledAppearanceType[];
  hearingRestriction: hearingRestrictionType[];
  document: civilDocumentType[];
  appearanceId: string;
  appearanceTime: string;
  binderText: string;
  courtListPrintSortNumber: string;
  civilDocumentsAvailable: string;
  appearanceDate: string;
  externalFileNumberText: string;
  courtListTypeCd: string;
  courtRoomCd: string;
  courtLevelCd: string;
  courtClassCd: string;
  fileAccessLevelCd: string;
  sealStartDate: string;
  sealEndDate: string;
  sheriffCommentText: string;
  sealFileSOCText: string;
  estimatedAppearanceMinutes: string;
  physicalFile: civilPhysicalFileType;
  asset: civilAssetType[];
  parties: civilPartiesType[];
  speaker: speakerType[];
  protectionOrder: [];
  arrestWarrant: arrestWarrantType[];
  unscheduledDocument: civilUnscheduledDocumentType[];
  orderToVary: civilOrderToVaryType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalCfcorderType {
  orderTypeDsc: string;
  cfcorderIssueDate: string;
  cfcconditionText: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalAgeNoticeType {
  eventDate: string;
  eventTypeDsc: string;
  detailText: string;
  dOB: string;
  relationship: string;
  provenBy: string;
  noticeTo: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalSentOrderToVaryType {
  formTypeCd: string;
  docmTypeDsc: string;
  docmId: string;
  docmIssueDate: string;
  docmImageId: string;
  docmStatus: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalBailOrderToVaryType {
  formTypeCd: string;
  docmTypeDsc: string;
  docmId: string;
  docmIssueDate: string;
  docmImageId: string;
  docmStatus: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalBansType {
  banTypeCd: string;
  banTypeDescription: string;
  banTypeAct: string;
  banTypeSection: string;
  banTypeSubSection: string;
  banStatuteId: string;
  banCommentText: string;
  banAcprId: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalAppearanceCountType {
  appearanceCountId: string;
  countPrintSequenceNumber: string;
  chargeStatuteCode: string;
  chargeStatuteDescription: string;
  lesserIncludedChargeStatuteCode: string;
  appearanceCountCancelledYN: string;
  lesserIncludedChargeStatuteDescription: string;
  appearanceReasonCode: string;
  pleaCode: string;
  pleaDate: string;
  electionCode: string;
  electionDate: string;
  mdctSeqNo: string;
  offenceAgeDaysNumber: string;
  issuingOfficerPoliceForceCode: string;
  issuingOfficerPINText: string;
  issuingOfficerSurnameName: string;
  findingCode: string;
  findingDate: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalAttendanceMethodType {
  apprId: string;
  assetUsageSeqNo: string;
  roleType: string;
  attendanceMethodCd: string;
  phoneNumber: string;
  instruction: string;
  otherRoleName: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalAccusedFormattedNameType {
  lastName: string;
  given1Name: string;
  given2Name: string;
  given3Name: string;
  orgName: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalIssueType {
  countPrntSeqNo: string;
  statuteActCd: string;
  statuteSectionCd: string;
  statuteSectionDsc: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalFileInformationType {
  fileLocaAgencyIdentifierCd: string;
  phyFileFolderNo: string;
  physTicketSeriesTxt: string;
  mdocInfoSeqNo: string;
  partId: string;
  profSeqNo: string;
  mdocJustinNo: string;
  courtLevelCd: string;
  courtClassCd: string;
  mdocTypeCd: string;
  mdocTypeDsc: string;
  mdocImageId: string;
  mdocAmendedYN: string;
  mdocAmendedText: string;
  issue: criminalIssueType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalCrownType {
  partId: string;
  assigned: boolean;
  fullName: string;
  lastNm: string;
  givenNm: string;
}

export interface criminalTrialRemarkType {
  commentTxt: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface criminalCourtListType {
  estimatedTimeHour: string;
  estimatedTimeMin: string;
  activityClassCd: string;
  activityClassDesc: string;
  inCustody: boolean;
  detained: boolean;
  video: boolean;
  remoteVideo: boolean;
  outOfTownJudge: string;
  supplementalEquipment: string;
  securityRestriction: string;
  appearanceReasonCd: string;
  appearanceReasonDesc: string;
  appearanceStatusCd: string;
  judgeInitials: string;
  adjudicatorNm: string;
  trialRemark: criminalTrialRemarkType[];
  trialRemarkTxt: string;
  crown: criminalCrownType[];
  scheduledAppearance: scheduledAppearanceType[];
  hearingRestriction: hearingRestrictionType[];
  criminalAppearanceID: string;
  courtListTypeCd: string;
  appearanceSequenceNumber: string;
  appearanceTime: string;
  fileInformation: criminalFileInformationType;
  fileNumberText: string;
  fileHomeLocationName: string;
  sealTypeCd: string;
  sealTypeDsc: string;
  otherFileInformationText: string;
  accusedFullName: string;
  accusedFormattedName: criminalAccusedFormattedNameType;
  accusedBirthDate: string;
  accusedCurrentBailProcessText: string;
  accusedInCustodyFlag: string;
  counselFullName: string;
  counselDesignationYN: string;
  counselPartId: string;
  caseAgeDaysNumber: string;
  crownTypeCd: string;
  crownLocationCd: string;
  participantRoleCd: string;
  participantRoleDsc: string;
  attendanceMethod: criminalAttendanceMethodType[];
  appearanceCount: criminalAppearanceCountType[];
  bans: criminalBansType[];
  bailOrderToVary: criminalBailOrderToVaryType[];
  sentOrderToVary: criminalSentOrderToVaryType[];
  ageNotice: criminalAgeNoticeType[];
  speaker: speakerType[];
  arrestWarrant: arrestWarrantType[];
  protectionOrder: protectionOrderType[];
  cfcorder: criminalCfcorderType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}
