import { criminalFileDetailsType } from "@/types/criminal/jsonTypes";
import { HttpService } from "./HttpService";

export class FilesService {
  private httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async searchCriminalFiles(query): Promise<criminalFileDetailsType[]> {
    return await this.httpService.get<criminalFileDetailsType[]>(`api/files/criminal/search?${query}`);
  }
}