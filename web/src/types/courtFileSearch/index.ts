import { chargeType } from "../criminal/jsonTypes";

// Intefaces
export interface CourtFileSearchCriteria {
  isCriminal: boolean;
  selectedFileNoOrParty: string;
  fileNumberTxt?: string;
  filePrefixTxt?: string;
  fileSuffixNo?: string;
  mDocRefTypeCode?: string;
  lastName?: string;
  givenName?: string;
  orgName?: string;
  class?: string;
  fileHomeAgencyId: string;
}

export interface CourtFileSearchResponse {
  recCount: number;
  responseCd: number;
  fileDetail: FileDetail[];
}

export interface FileDetail {
  mdocJustinNo: string;
  physicalFileId: string;
  fileHomeAgencyId: string;
  fileNumberTxt: string;
  mdocSeqNo: string;
  courtLevelCd: string;
  courtClassCd: string;
  warrantYN: string;
  inCustodyYN: string;
  nextApprDt: string;
  pcssCourtDivisionCd: string;
  sealStatusCd: string;
  approvalCrownAgencyTypeCd: string;
  participant: Participant[];
}

export interface Participant {
  fullNm: string;
  charge: chargeType[];
}

// Enums
export enum CourtClassEnum {
  A = 0, // Adult
  Y = 1, // Youth
  T = 2, // Trafic
  F = 3, // Family
  C = 4, // Small Claims
  M = 5, // Motor Vehicle
  L = 6  // Enforcement/Legistated Statute
}

export enum SearchModeEnum {
  FileNo = 0,
  PartName = 1,
}