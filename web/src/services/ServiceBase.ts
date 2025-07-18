import { IHttpService } from './HttpService';

export class ServiceBase {
  constructor(protected httpService: IHttpService) {
    this.httpService = httpService;
  }
}
