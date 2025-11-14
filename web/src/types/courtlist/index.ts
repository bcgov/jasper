import { CourtRoomsInfo, IconStyleType } from '../common';
import { courtListType } from '../courtlist/jsonTypes';

export interface LocationInfo {
  name: string;
  shortName: string;
  code: string;
  locationId: string;
  active?: boolean;
  agencyIdentifierCd: string;
  courtRooms: CourtRoomsInfo[];
}

export interface courtListInformationInfoType {
  detailsData: courtListType;
}

export interface roomsInfoType {
  value: string;
  text: string;
}

export interface courtRoomsAndLocationsInfoType {
  value: locationInfoType;
  text: string;
}

export interface locationInfoType {
  Location: string;
  LocationID: string;
  Rooms: roomsInfoType[];
}

export interface civilListInfoType {
  index: string;
  seq: number;
  fileNumber: string;
  tag: string;
  icons: IconStyleType[];
  time: string;
  room: string;
  parties: string;
  partiesTruncApplied: boolean;
  partiesDesc: string;
  reason: string;
  reasonDesc: string;
  est: string;
  supplementalEquipment: string;
  securityRestriction: string;
  outOfTownJudge: string;
  counsel: string;
  counselDesc: string;
  fileId: string;
  appearanceId: string;
  fileMarkers: fileMarkerInfoType[];
  hearingRestrictions: hearingRestrictionInfoType[];
  notes: civilNotesInfoType;
  noteExist: boolean;
  listClass: string;
}

export interface hearingRestrictionInfoType {
  abbr: string;
  key: string;
}

export interface fileMarkerInfoType {
  abbr: string;
  key: string;
}

export interface civilNotesInfoType {
  TrialNotes: string;
  FileComment: string;
  CommentToJudge: string;
  SheriffComment: string;
}

export interface criminalListInfoType {
  index: string;
  seq: number;
  fileNumber: string;
  tag: string;
  icons: IconStyleType[];
  caseAge: string;
  time: string;
  room: string;
  accused: string;
  accusedTruncApplied: boolean;
  accusedDesc: string;
  reason: string;
  reasonDesc: string;
  crown: string;
  crownDesc: string;
  est: string;
  supplementalEquipment: string;
  securityRestriction: string;
  outOfTownJudge: string;
  courtLevel?: string;
  courtClass?: string;
  profSeqNo?: string;
  counsel: string;
  // CounselDesc: string;
  partId: string;
  justinNo: string;
  appearanceId: string;
  fileMarkers: fileMarkerInfoType[];
  hearingRestrictions: hearingRestrictionInfoType[];
  trialNotes: string;
  trialRemarks: trialRemarkInfoType[];
  notes: criminalNotesInfoType;
  noteExist: boolean;
  listClass: string;
}

export interface criminalNotesInfoType {
  remarks: trialRemarkInfoType[];
  text: string;
}

export interface trialRemarkInfoType {
  txt: string;
}

export interface courtListInfoType {
  index: string;
  seq: number;
  fileNumber: string;
  tag: string;
  icons: IconStyleType[];
  time: string;
  room: string;
  parties?: string;
  partiesTruncApplied?: boolean;
  partiesDesc?: string;
  accused?: string;
  accusedTruncApplied?: boolean;
  accusedDesc?: string;
  caseAge?: string;
  crown?: string;
  crownDesc?: string;
  courtLevel?: string;
  courtClass?: string;
  profSeqNo?: string;
  partId?: string;
  justinNo?: string;
  trialNotes?: string;
  trialRemarks?: trialRemarkInfoType[];
  reason: string;
  reasonDesc: string;
  est: string;
  supplementalEquipment: string;
  securityRestriction: string;
  outOfTownJudge: string;
  counsel: string;
  counselDesc: string;
  fileId: string;
  appearanceId: string;
  fileMarkers: fileMarkerInfoType[];
  hearingRestrictions: hearingRestrictionInfoType[];
  notes: courtNotesInfoType;
  noteExist: boolean;
  listClass: string;
}

export interface courtNotesInfoType {
  TrialNotes?: string;
  FileComment?: string;
  CommentToJudge?: string;
  SheriffComment?: string;
  remarks?: trialRemarkInfoType[];
  text?: string;
}

export interface CourtListCardInfo {
  shortHandDate: string;
  courtListLocation: string;
  courtListLocationID: number;
  courtListRoom: string;
  totalCases: number;
  totalTime: string;
  totalTimeUnit: string;
  criminalCases: number;
  familyCases: number;
  civilCases: number;
  amPM: string;
  fileCount: number;
  presider: string;
  activity: string;
  courtClerk: string;
  email?: string;
}

export interface CourtListAppearance {
  aslSortOrder?: number;
  courtDivisionCd: string;
  appearanceDt: string;
  appearanceTm: string;
  courtRoomCd: string;
  courtFileNumber: string;
  pcssAppearanceId?: number;
  isComplete?: boolean;
  activityClassCd: string;
  activityClassDsc: string;
  appearanceReasonCd: string;
  appearanceReasonDsc: string;
  appearanceMethod?: AppearanceMethod;
  equipmentBooking?: any;
  scheduleNoteTxt: string;
  estimatedTimeHour: string;
  estimatedTimeMin: string;
  estimatedTimeString: string;
  justinNo: string;
  physicalFileId: string;
  courtlistRefNumber: string;
  styleOfCause: string;
  adjudicatorInitials: string;
  adjudicatorNm: string;
  caseAgeDays: string;
  videoYn: string;
  accusedNm: string;
  accusedCounselNm: string;
  appearanceId: string;
  profPartId: string;
  profSeqNo: string;
  inCustodyYn: string;
  detainedYn: string;
  continuationYn: string;
  condSentenceOrderYn: string;
  lackCourtTimeYn: string;
  otherFactorsYn: string;
  otherFactorsComment: string;
  cfcsaYn: string;
  softYn: string;
  scheduledOnDt: string;
  scheduledByInitials: string;
  scheduledByName: string;
  activityCd: string;
  activityDsc: string;
  courtActivityId?: number;
  courtActivitySlotId?: number;
  remoteVideoYn: string;
  appearanceStatusCd: string;
  appearanceStatusDsc: string;
  totalAppearances?: number;
  appearanceNumber?: number;
  trialTrackerCd: string;
  trialTrackerDsc: string;
  trialTrackerTrialResultTxt: string;
  trialTrackerOtherTxt: string;
  aslParentTrialTrackerCd: string;
  aslParentTrialTrackerDsc: string;
  assignmentListRoomYn: string;
  aslChildAppearance?: any;
  charges?: any[];
  crown?: Crown[];
  counsel?: PcssCounsel[];
  justinCounsel?: JustinCounsel;
  homeLocationId?: number;
  homeLocationNm: string;
  remoteLocationId?: number;
  remoteLocationNm: string;
  ceisCounsel?: any;
  justinApprId: string;
  ceisAppearanceId: string;
  jcmComments?: JcmComment2[];
  appearanceAdjudicatorRestriction?: AppearanceAdjudicatorRestriction[];
  stoodDownJCMYn: string;
  courtClassCd: string;
  appearanceSequenceNumber: string;
  fixedListDoneYn: string;
  aslCourtFileNumber: string;
  selfRepresentedYn: string;
  otherRepresentedYn: string;
  linkedCounsel?: PcssCounsel;
  aslFeederAdjudicators?: any[];
}

export interface AppearanceMethod {
  responseMessageTxt: string;
  responseCd: string;
  courtDivisionCd: string;
  details?: AppearanceMethodDetail[];
}

export interface AppearanceMethodDetail {
  appearanceId: string;
  roleTypeCd: string;
  appearanceMethodCd: string;
  assetUsageSeqNo: string;
  phoneNumberTxt: string;
  instructionTxt: string;
  apprMethodCcn: string;
  origRoleCd: string;
  origAppearanceMethodCd: string;
}

export interface Crown {
  partId: string;
  lastNm: string;
  givenNm: string;
  assignedYn: string;
}

export interface PcssCounsel {
  counselId?: number;
  lawSocietyId?: number;
  lastNm: string;
  givenNm: string;
  prefNm: string;
  addressLine1Txt: string;
  addressLine2Txt: string;
  cityTxt: string;
  province: string;
  postalCode: string;
  phoneNoTxt: string;
  emailAddressTxt: string;
  activeYn: string;
  counselType: string;
  orgNm: string;
  justinCounsel?: JustinCounsel;
  jcmComments?: JcmComment2[];
  activityAppearanceDetail?: any[];
}

export interface JustinCounsel {
  lastNm: string;
  givenNm: string;
  counselEnteredDt: string;
  counselPartId: string;
  counselRelatedRepTypeCd: string;
  counselRrepId: string;
}

export interface JcmComment2 {
  jcmCommentId?: number;
  justinNo: string;
  physicalFileId: string;
  commentTxt: string;
  entDtm: string;
  updDtm: string;
  rotaInitialsCd: string;
  fullName: string;
}

export interface AppearanceAdjudicatorRestriction {
  appearanceAdjudicatorRestrictionId: number;
  hearingRestrictionId: number;
  hearingRestrictionCd: string;
  judgeId: number;
  judgesInitials: string;
  fileNoTxt: string;
  hearingRestrictionTxt: string;
  hasIssue: boolean;
}
