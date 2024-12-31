import { Location } from '@/types';
import { CourtRoomsJsonInfoType } from '../types/common';
import { roomsInfoType } from '../types/courtlist';
import { HttpService } from './HttpService';

export class LocationService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getCourtRooms(): Promise<roomsInfoType[]> {
    const courtRoomsJson = await this.httpService.get<CourtRoomsJsonInfoType[]>(
      'api/location/court-rooms'
    );

    courtRoomsJson.sort((a, b) =>
      a.name.toLocaleLowerCase().localeCompare(b.name.toLowerCase())
    );
    const courtRooms: roomsInfoType[] = [];

    courtRoomsJson.forEach((cr) => {
      courtRooms.push({
        text: cr.name,
        value: cr.code,
      });
    });

    return courtRooms;
  }

  async getDashboardLocations(): Promise<Location[]> {
    return await this.httpService.get<Location[]>('api/location/pcss');
  }
}
