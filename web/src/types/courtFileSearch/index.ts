import { chargeType } from "../criminal/jsonTypes";

// Intefaces
export interface CourtFileSearchCriteria {
  isCriminal: boolean;
  isFamily: boolean;
  isSmallClaims: boolean;
  searchBy: string;
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
  ticketSeriesTxt: string;
  mdocRefTypeCd: string;
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

export enum SearchModeEnum {
  FileNo = 0,
  PartName = 1,
}