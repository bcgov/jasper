import { PersonSearchItem } from '@/types';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class JudgeService extends ServiceBase {
  private readonly baseUrl: string = 'api/judges';

  constructor(httpService: IHttpService) {
    super(httpService);
  }

  getJudges(): Promise<PersonSearchItem[]> {
    return this.httpService.get<PersonSearchItem[]>(
      this.baseUrl,
      {},
      { skipErrorHandler: true }
    );
  }
}
