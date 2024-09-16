import { CourtRoomsJsonInfoType } from "../types/common";
import { roomsInfoType } from "../types/courtlist";
import { HttpService } from "./HttpService";

export class LocationService {
  private httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getCourtRooms(): Promise<roomsInfoType[]> {
    const courtRoomsJson = await this.httpService.get<CourtRoomsJsonInfoType[]>('api/location/court-rooms');

    courtRoomsJson.sort((a, b) => a.name.toLocaleLowerCase().localeCompare(b.name.toLowerCase()))
    const courtRooms: roomsInfoType[] = []

    courtRoomsJson.map(cr => {
      courtRooms.push({
        text: cr.name,
        value: cr.code
      })
    });

    return courtRooms;
  }
}