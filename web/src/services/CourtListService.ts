import { ApiResponse } from '@/types/ApiResponse';
import { courtListType } from '@/types/courtlist/jsonTypes';
import {
  CourtListDocumentBundleRequest,
  CourtListDocumentBundleResponse,
} from '@/types/courtlist/jsonTypes';
import { HttpService } from './HttpService';

export class CourtListService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  generateReportUrl(params: Record<string, any> = {}): string {
    return this.httpService.client.getUri({
      url: 'api/courtlist/generate-report',
      params,
    });
  }

  async getCourtList(
    agencyId: string | null,
    roomCode: string | null,
    proceeding: string,
    judgeId: number | null
  ): Promise<courtListType> {
    const url = `api/courtlist`;
    const params = {
      agencyId: agencyId,
      roomCode,
      proceeding,
      judgeId: judgeId ?? '',
    };
    return this.httpService.client
      .get<courtListType>(url, { params })
      .then((res) => res.data);
  }

  async generateCourtListPdf(
    bundleRequest: CourtListDocumentBundleRequest
  ): Promise<ApiResponse<CourtListDocumentBundleResponse>> {
    return this.httpService.post<ApiResponse<CourtListDocumentBundleResponse>>(
      `api/courtlist/document-bundle`,
      bundleRequest
    );
  }
}
