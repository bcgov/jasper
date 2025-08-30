import { UserInfo } from '@/types/common';
import { HttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class UserService extends ServiceBase {
  constructor(httpService: HttpService) {
    super(httpService);
  }

  async requestAccess(): Promise<UserInfo> {
    return await this.httpService.put<UserInfo>(`api/users/request-access`, {
      skipErrorHandler: true,
    });
  }

  async getMyUser(): Promise<UserInfo> {
    return await this.httpService.get<UserInfo>(`api/users/me`);
  }
}
