import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';
import { ReservedJudgement } from '@/types/ReservedJudgement';

export class ReservedJudgementService extends ServiceBase {
  constructor(httpService: IHttpService) {
    super(httpService);
  }

  get(
    queryParams: Record<string, any> | undefined
  ): Promise<ReservedJudgement[]> {
    return this.httpService.get<ReservedJudgement[]>(
      `api/reservedJudgements`,
      queryParams
    );
  }
}
