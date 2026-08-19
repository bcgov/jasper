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

  async markReleaseNotesViewed(version: string): Promise<void> {
    await this.httpService.post<void>(`api/users/me/release-notes`, {
      version,
    });
  }

  getSignature(): Promise<Blob> {
    return this.httpService.get<Blob>(
      `api/users/me/signature`,
      {},
      { responseType: 'blob' }
    );
  }

  getInitials(): Promise<Blob> {
    return this.httpService.get<Blob>(
      `api/users/me/initials`,
      {},
      { responseType: 'blob' }
    );
  }
}
