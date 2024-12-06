import { LookupCode } from '../types/common';
import { HttpService } from './HttpService';

export class LookupService {
  private httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getCourtClasses(): Promise<LookupCode[]> {
    return await this.httpService.get<LookupCode[]>('api/codes/court/classes');
  }
}
