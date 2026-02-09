import { ApiResponse } from '@/types/ApiResponse';
import { CourtListSearchResult } from '@/types/courtlist';
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
  ): Promise<ApiResponse<CourtListSearchResult>> {
    const url = `api/courtlist`;
    const params = {
      agencyId: agencyId,
      roomCode,
      proceeding,
      judgeId: judgeId ?? '',
    };
    return this.httpService.client
      .get<ApiResponse<CourtListSearchResult>>(url, { params })
      .then((res) => res.data);
  }
}
