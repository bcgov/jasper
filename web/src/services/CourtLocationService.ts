import { CourtLocation } from '@/types/CourtLocation';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class CourtLocationService extends ServiceBase {
  private readonly baseUrl: string = 'api/court-locations';

  constructor(httpService: IHttpService) {
    super(httpService);
  }

  getCourtLocationByCode(code: string): Promise<CourtLocation> {
    return this.httpService.get<CourtLocation>(
      `${this.baseUrl}?code=${encodeURIComponent(code)}`,
      {},
      { skipErrorHandler: true }
    );
  }
}
