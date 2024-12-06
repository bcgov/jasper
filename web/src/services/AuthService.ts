import { UserInfo } from '@/types/common';
import { HttpService } from './HttpService';

export class AuthService {
  readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getUserInfo(): Promise<UserInfo> {
    return await this.httpService.get<UserInfo>(`api/auth/info`);
  }
}
