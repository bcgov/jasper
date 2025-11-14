import { TimebankSummary, VacationPayout } from '@/types/timebank';
import { HttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class TimebankService extends ServiceBase {
  constructor(httpService: HttpService) {
    super(httpService);
  }

  async getTimebankSummaryForJudge(
    period: number,
    judgeId?: number,
    includeLineItems: boolean = true
  ): Promise<TimebankSummary | null> {
    const params = new URLSearchParams();

    if (judgeId !== undefined) {
      params.append('judgeId', judgeId.toString());
    }

    params.append('includeLineItems', includeLineItems.toString());

    const queryString = params.toString();
    let url = `api/timebank/summary/${period}`;
    if (queryString) {
      url += `?${queryString}`;
    }

    return await this.httpService.get<TimebankSummary>(url);
  }

  async getTimebankPayoutForJudge(
    period: number,
    expiryDate: Date,
    rate: number,
    judgeId?: number
  ): Promise<VacationPayout> {
    const params = new URLSearchParams();

    if (judgeId !== undefined) {
      params.append('judgeId', judgeId.toString());
    }

    params.append('expiryDate', expiryDate.toISOString());
    params.append('rate', rate.toString());

    const queryString = params.toString();
    let url = `api/timebank/payout/${period}`;
    if (queryString) {
      url += `?${queryString}`;
    }

    return await this.httpService.get<VacationPayout>(url);
  }
}
