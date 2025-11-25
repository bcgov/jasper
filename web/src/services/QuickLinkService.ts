import { QuickLink } from '@/types';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class QuickLinkService extends ServiceBase {
  private readonly baseUrl: string = 'api/quick-links';

  constructor(httpService: IHttpService) {
    super(httpService);
  }

  getQuickLinks = (): Promise<QuickLink[]> =>
    this.httpService.get<QuickLink[]>(this.baseUrl);
}
