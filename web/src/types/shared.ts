import { AdditionalProperties } from '@/types/common';

export interface DocumentData {
  appearanceDate?: string;
  appearanceId?: string;
  courtClass?: string;
  courtLevel: string;
  dateFiled?: string;
  documentDescription?: string;
  documentId?: string;
  fileId?: string;
  fileNumberText: string;
  location: string;
  partId?: string;
  partyName?: string;
  profSeqNo?: string;
}

export enum CourtDocumentType {
  Civil,
  ProvidedCivil,
  CSR,
  Criminal,
  ROP,
  CriminalZip,
  CivilZip,
}

export interface FileDetailsType {
  responseCd: string;
  responseMessageTxt: string;
  fileNumberTxt: string;
  courtClassCd: string;
  courtClassDescription: string;
  courtLevelCd: string;
  courtLevelDescription: string;
  appearances: AppearancesType;
  activityClassCd: string;
  activityClassDesc: string;
  homeLocationAgenId: string;
  homeLocationAgencyName: string;
  homeLocationAgencyCode: string;
  homeLocationRegionName: string;
}

export interface AppearancesType {
  responseCd: string;
  responseMessageTxt: string;
  futureRecCount: string;
  historyRecCount: string;
  apprDetail: ApprDetailType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface ApprDetailType {
  historyYN: string;
  appearanceId: string;
  appearanceDt: string;
  appearanceTm: string;
  appearanceReasonCd: string;
  courtAgencyId: string;
  courtRoomCd: string;
  judgeFullNm: string;
  judgeInitials: string;
  estimatedTimeHour: string;
  estimatedTimeMin: string;
  partOfTrialYN: string;
  appearanceStatusCd: string;
  appearanceResultCd: string;
  appearanceReasonDsc?: string;
  appearanceCcn: string;
  courtLocation?: string;
  supplementalEquipmentTxt: string;
  securityRestrictionTxt: string;
  outOfTownJudgeTxt: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface PersonType {
  givenNm: string;
  lastNm: string;
}

export interface DataTableHeader {
  key: string;
  title?: string;
  value?: (item: any) => string;
  sortRaw?: (a: any, b: any) => number;
  [key: string]: any;
}
