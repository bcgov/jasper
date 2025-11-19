import { HttpService } from './HttpService';

export interface DarsLogNote {
  date: string;
  agencyIdentifierCd: number | null;
  courtRoomCd: string;
  url: string;
  fileName: string;
  locationNm: string;
}

export class DarsService {
  constructor(private readonly httpService: HttpService) {}

  async search(
    date: string,
    agencyIdentifierCd: string,
    courtRoomCd: string
  ): Promise<DarsLogNote[]> {
    return this.httpService.get<DarsLogNote[]>(
      'api/Dars/search',
      {
        date,
        agencyIdentifierCd,
        courtRoomCd,
      },
      { skipErrorHandler: true }
    );
  }
}
