import { CourtRoomsInfo, IconStyleType } from '../common';
import { courtListType } from '../courtlist/jsonTypes';

export interface LocationInfo {
  name: string;
  code: string;
  locationId: string;
  active?: boolean;
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