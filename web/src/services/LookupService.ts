import { LookupCode } from '../types/common';
import { HttpService } from './HttpService';

export class LookupService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getCourtClasses(): Promise<LookupCode[]> {
    return await this.httpService.get<LookupCode[]>('api/codes/court/classes');
  }

  getRoleTypes(): Promise<LookupCode[]> {
    return this.httpService.get<LookupCode[]>('api/codes/roles');
  }
}
