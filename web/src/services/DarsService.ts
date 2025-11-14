import { HttpService } from './HttpService';

export interface DarsLogNote {
  date: string;
  locationId: number | null;
  courtRoomCd: string;
  url: string;
  fileName: string;
  locationNm: string;
}

export class DarsService {
  constructor(private readonly httpService: HttpService) {}

  async search(
    date: string,
    locationId: number,
    courtRoomCd: string
  ): Promise<DarsLogNote[]> {
    return this.httpService.get<DarsLogNote[]>('api/Dars/search', {
      date,
      locationId,
      courtRoomCd,
    });
  }
}
