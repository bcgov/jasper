import { DocumentRequestType } from "@/types/shared";

export interface GeneratePdfResponse {
  base64Pdf: string;
  pageRanges: Array<[number, number]>;
}

export type GeneratePdfRequest = {
  type: DocumentRequestType;
  data: {
    partId: string;
    profSeqNo: string;
    courtLevelCd: string;
    courtClassCd: string;
    requestAgencyIdentifierId: string;
    requestPartId: string;
    applicationCd: string;
    appearanceId: string;
    reportName: string;
    documentId: string;
    courtDivisionCd: string;
    fileId: string;
    flatten: boolean;
    correlationId: string;
    date?: Date;
    locationId?: number;
    roomCode?: string;
    additionsList?: string;
    reportType?: string;
  };
};