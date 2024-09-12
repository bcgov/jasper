import { CriminalCourtFileSearchResult } from "@/types/courtFileSearch";
import { HttpService } from "./HttpService";

export class FilesService {
  private httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async searchCriminalFiles(query): Promise<CriminalCourtFileSearchResult> {
    return await this.httpService.get<CriminalCourtFileSearchResult>(`api/files/criminal/search?${query}`);
  }
}