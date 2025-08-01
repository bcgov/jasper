import { PersonSearchItem } from '@/types';
import { HttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class UserService extends ServiceBase {
  constructor(httpService: HttpService) {
    super(httpService);
  }

  getJudges(): Promise<PersonSearchItem[]> {
    return this.httpService.get<PersonSearchItem[]>(`api/users/judges`);
  }
}
