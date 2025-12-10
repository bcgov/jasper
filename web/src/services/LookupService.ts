import { LookupCode } from '../types/common';
import { HttpService } from './HttpService';

export class LookupService {
  private readonly httpService: HttpService;
  private readonly baseUrl: string = 'api/codes';

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getCourtClasses(): Promise<LookupCode[]> {
    return await this.httpService.get<LookupCode[]>(
      `${this.baseUrl}/court/classes`
    );
  }

  getRoles(): Promise<LookupCode[]> {
    return this.httpService.get<LookupCode[]>(`${this.baseUrl}/roles`);
  }
}
