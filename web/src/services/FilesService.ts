import { CivilAppearanceDetails } from '@/types/civil/jsonTypes/index';
import { CourtFileSearchResponse } from '@/types/courtFileSearch';
import { CriminalAppearanceDetails } from '@/types/criminal/jsonTypes/index';
import { HttpService } from './HttpService';
import { GeneratePdfResponse, GeneratePdfRequest } from '@/components/documents/models/GeneratePdf';
import { ApiResponse } from '@/types/ApiResponse';
import { Binder } from '@/types';

export class FilesService {
  private httpService: HttpService;
  private readonly baseUrl: string = 'api/files';

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async searchCriminalFiles(
    queryParams: any
  ): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(
      `${this.baseUrl}/criminal/search`,
      queryParams
    );
  }

  async searchCivilFiles(queryParams: any): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(
      `${this.baseUrl}/civil/search?`,
      queryParams
    );
  }

  async civilAppearanceDetails(
    fileId: string,
    appearanceId: string
  ): Promise<CivilAppearanceDetails> {
    return this.httpService.get<any>(
      `${this.baseUrl}/civil/${fileId}/appearance-detail/${appearanceId}`
    );
  }

  async criminalAppearanceDetails(
    fileId: string,
    appearanceId: string,
    partId: string
  ): Promise<CriminalAppearanceDetails> {
    return this.httpService.get<any>(
      `${this.baseUrl}/criminal/${fileId}/appearance-detail/${appearanceId}/${partId}`
    );
  }

  async generatePdf(
    requestData: GeneratePdfRequest[]
  ): Promise<GeneratePdfResponse> {
    return this.httpService.post<GeneratePdfResponse>(
      `${this.baseUrl}/document/generate-pdf`,
      requestData
    );
  }

  async generateCourtListPdf(bundleRequest: CourtListDocumentBundleRequest[]): Promise<ApiResponse<CourtListDocumentBundleResponse>> {
    // mock for now
    return {} as ApiResponse<CourtListDocumentBundleResponse>;
  }

  // Coming soon...
  // async PCSSCriminalAppearanceDetails(
  //   fileId: string,
  //   appearanceId: string,
  //   partId: string,
  //   seqNo: string
  // ): Promise<CriminalAppearanceDetails> {
  //   //?partId=127956.0102&profSeqNo=31
  //   return this.httpService.get<any>(
  //     `${this.baseUrl}/pcss/criminal/${fileId}/appearance-detail/${appearanceId}/?partId=${partId}&profSeqNo=${seqNo}`
  //   );
  // }
}

export interface CourtListDocumentBundleRequest {
  appearances: CourtListAppearanceDocumentRequest[];
}

export interface CourtListAppearanceDocumentRequest {
  fileId: string;
  appearanceId: string;
  participantId: string;
  courtClassCd: string;
}

export interface CourtListDocumentBundleResponse {
  binders: Binder[];
  pdfDocumentResponse: GeneratePdfResponse[];
  participantId: string;
  courtClassCd: string;
}