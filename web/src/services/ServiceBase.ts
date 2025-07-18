import { HttpService } from './HttpService';

export class ServiceBase {
  protected readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }
}
