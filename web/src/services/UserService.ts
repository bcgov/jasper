import { UserInfo } from '@/types/common';
import { HttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class UserService extends ServiceBase {
  constructor(httpService: HttpService) {
    super(httpService);
  }

  async requestAccess(email: string): Promise<UserInfo> {
    return await this.httpService.get<UserInfo>(
      `api/users/request-access`,
      {
        email: email,
      },
      { skipErrorHandler: true }
    );
  }

  async getMyUser(): Promise<UserInfo> {
    return await this.httpService.get<UserInfo>(`api/users/me`);
  }
}
