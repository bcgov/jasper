import { CourtFileSearchResponse } from "@/types/courtFileSearch";
import { HttpService } from "./HttpService";

export class FilesService {
  private httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async searchCriminalFiles(query): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(`api/files/criminal/search?${query}`);
  }

  async searchCivilFiles(query): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(`api/files/civil/search?${query}`);
  }
}