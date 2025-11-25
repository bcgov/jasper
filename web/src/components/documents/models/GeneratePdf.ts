import { DocumentRequestType } from '@/types/shared';

export interface GeneratePdfResponse {
  base64Pdf: string;
  pageRanges: Array<PageRange>;
}
export interface PageRange {
  start: number;
  end: number;
}

export type GeneratePdfRequest = {
  type: DocumentRequestType;
  data: {
    partId: string;
    profSeqNo: string;
    courtLevelCd: string;
    courtClassCd: string;
    appearanceId: string;
    documentId: string;
    courtDivisionCd: string;
    fileId: string;
    isCriminal: boolean;
    correlationId: string;
    date?: Date;
    locationId?: number;
    roomCode?: string;
    additionsList?: string;
    reportType?: string;
    orderId?: string;
    transcriptDocumentId?: string;
  };
};
