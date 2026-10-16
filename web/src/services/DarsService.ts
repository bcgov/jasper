import { HttpService } from './HttpService';

export interface DarsLogNote {
  date: string;
  agencyIdentifierCd: number | null;
  courtRoomCd: string;
  url: string;
  fileName: string;
  locationNm: string;
}

export interface TranscriptAppearance {
  appearanceDt: string;
  appearanceReasonCd: string;
  appearanceTm: string;
  justinAppearanceId: number;
  ceisAppearanceId: number;
  courtAgencyId: number;
  courtRoomCd: string;
  judgeFullNm: string;
  estimatedDuration: number;
  estimatedStartTime: string;
  fileid: number;
  id: number;
  isInCamera: boolean;
  statusCodeId: number;
}

export interface TranscriptDocument {
  id: number;
  orderId: number;
  description: string;
  fileName: string;
  pagesComplete: number;
  statusCodeId: number;
  appearances: TranscriptAppearance[];
}

export class DarsService {
  constructor(private readonly httpService: HttpService) {}

  async search(
    date: string,
    agencyIdentifierCd: string,
    courtRoomCd: string
  ): Promise<DarsLogNote[]> {
    return this.httpService.get<DarsLogNote[]>(
      'api/Dars/search',
      {
        date,
        agencyIdentifierCd,
        courtRoomCd,
      },
      { suppressErrorNotification: true }
    );
  }

  async getTranscripts(
    physicalFileId?: string,
    mdocJustinNo?: string
  ): Promise<TranscriptDocument[]> {
    return this.httpService.get<TranscriptDocument[]>(
      'api/Dars/transcripts',
      {
        physicalFileId,
        mdocJustinNo,
        returnChildRecords: true,
      },
      { suppressErrorNotification: true }
    );
  }

  async getTranscriptDocument(
    orderId: string,
    documentId: string
  ): Promise<string> {
    const response = await this.httpService.get<{ base64Pdf: string }>(
      `api/Dars/transcript/${orderId}/${documentId}`,
      {}
    );
    return response.base64Pdf;
  }
}
