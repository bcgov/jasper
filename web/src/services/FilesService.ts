import { CourtFileSearchResponse } from "@/types/courtFileSearch";
import { HttpService } from "./HttpService";

export class FilesService {
  private httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async searchCriminalFiles(queryParams: any): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(`api/files/criminal/search`, queryParams);
  }

  async searchCivilFiles(queryParams: any): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(`api/files/civil/search?`, queryParams);
  }
}