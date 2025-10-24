import { ApplicationInfo } from '@/types/common';
import { HttpService } from './HttpService';

export class ApplicationService {
  readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getApplicationInfo(): Promise<ApplicationInfo> {
    return await this.httpService.get<ApplicationInfo>(`api/application/info`);
  }
}
