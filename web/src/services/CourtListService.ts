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
}
