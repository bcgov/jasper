import { Location } from '@/types';
import { HttpService } from './HttpService';

export class LocationService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getLocations(includeChildRecords = false): Promise<Location[]> {
    return await this.httpService.get<Location[]>(`api/location?includeChildRecords=${includeChildRecords}`);
  }
}
