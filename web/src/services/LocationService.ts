import { LocationInfo } from '@/types/courtlist';
import { HttpService } from './HttpService';

export class LocationService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getLocations(includeChildRecords = false): Promise<LocationInfo[]> {
    return await this.httpService.get<LocationInfo[]>(
      `api/location?includeChildRecords=${includeChildRecords}`
    );
  }
}
