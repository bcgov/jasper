import { CustomAxiosConfig } from '@/types';
import { AccessRequestResponse, UserRecord } from '@/types/common';
import { HttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class UserService extends ServiceBase {
  constructor(httpService: HttpService) {
    super(httpService);
  }

  async requestAccess(): Promise<AccessRequestResponse> {
    return await this.httpService.put<AccessRequestResponse>(
      `api/users/request-access`,
      {},
      {},
      'json',
      { suppressErrorNotification: true }
    );
  }

  async getMyUser(config: CustomAxiosConfig = {}): Promise<UserRecord> {
    return await this.httpService.get<UserRecord>(`api/users/me`, {}, config);
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
