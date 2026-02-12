import { ApiResponse } from '@/types/ApiResponse';
import { CourtListSearchResult } from '@/types/courtlist';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class CourtListService extends ServiceBase {
  constructor(httpService: IHttpService) {
    super(httpService);
  }

  generateReportUrl(params: Record<string, any> = {}): string {
    return this.httpService.getUri({
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
    return this.httpService.get<ApiResponse<CourtListSearchResult>>(
      `api/courtlist`,
      {
        agencyId,
        roomCode,
        proceeding,
        judgeId: judgeId ?? '',
      },
      { skipErrorHandler: true }
    );
  }
}
