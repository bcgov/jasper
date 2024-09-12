// Intefaces
export interface CourtFileSearchCriteria {
  isCriminal: boolean;
  selectedFileNoOrParty: string;
  fileNumber?: string;
  prefix?: string;
  seqNum?: string;
  typeRef?: string;
  surname?: string;
  givenName?: string;
  org?: string;
  class?: string;
  location: string;
}

export interface CriminalCourtFileSearchResult {
  recCount: number;
  responseCd: number;
  fileDetail: CriminalCourtFileSearchDetail[];
}

export interface CriminalCourtFileSearchDetail {
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
  participant: CriminalCourtFileSearchParticipant[];
}

export interface CriminalCourtFileSearchParticipant {
  fullNm: string;
  charge: CriminalCourtFileSearchCharge[];
}

export interface CriminalCourtFileSearchCharge {
  sectionTxt: string;
  sectionDscTxt: string;
}

// Enums
export enum CourtClassEnum {
  Adult = "A",
  Family = "F",
  SmallClaims = "C",
  Youth = "Y"
}

export enum SearchModeEnum {
  FileNo = "FILENO",
  PartName = "PARTNAME",
}