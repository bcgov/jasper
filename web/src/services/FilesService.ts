import {
  GeneratePdfRequest,
  GeneratePdfResponse,
} from '@/components/documents/models/GeneratePdf';
import { CivilAppearanceDetailParty, CivilAppearanceDetailDocuments, CivilAppearanceDetailMethods } from '@/types/civil/jsonTypes/index';
import { CourtFileSearchResponse } from '@/types/courtFileSearch';
import { CriminalAppearanceDetails, CriminalAppearanceDocuments } from '@/types/criminal/jsonTypes/index';
import { HttpService } from './HttpService';

export class FilesService {
  private readonly httpService: HttpService;
  private readonly baseUrl: string = 'api/files';

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async searchCriminalFiles(
    queryParams: any
  ): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(
      `${this.baseUrl}/criminal/search`,
      queryParams
    );
  }

  async searchCivilFiles(queryParams: any): Promise<CourtFileSearchResponse> {
    return await this.httpService.get<CourtFileSearchResponse>(
      `${this.baseUrl}/civil/search?`,
      queryParams
    );
  }

  async civilAppearanceDocuments(
    fileId: string,
    appearanceId: string,
    includeJudicialBinder: boolean = false
  ): Promise<CivilAppearanceDetailDocuments> {
    return this.httpService.get<any>(
      `${this.baseUrl}/civil/${fileId}/appearance/${appearanceId}/documents?includeJudicialBinder=${includeJudicialBinder}`
    );
  }

  async civilAppearanceParty(
    fileId: string,
    appearanceId: string,
  ): Promise<CivilAppearanceDetailParty> {
    return this.httpService.get<any>(
      `${this.baseUrl}/civil/${fileId}/appearance/${appearanceId}/party`
    );
  }

  async civilAppearanceMethods(
    fileId: string,
    appearanceId: string,
  ): Promise<CivilAppearanceDetailMethods> {
    return this.httpService.get<any>(
      `${this.baseUrl}/civil/${fileId}/appearance/${appearanceId}/methods`
    );
  }

  async criminalAppearanceDetails(
    fileId: string,
    appearanceId: string,
    partId: string
  ): Promise<CriminalAppearanceDetails> {
    return this.httpService.get<any>(
      `${this.baseUrl}/criminal/${fileId}/appearance-detail/${appearanceId}/${partId}`
    );
  }

  async criminalAppearanceDocuments(
    fileId: string,
    partId: string
  ): Promise<CriminalAppearanceDocuments> {
    return this.httpService.get<any>(
      `${this.baseUrl}/criminal/${fileId}/appearance-detail/${partId}/documents`
    );
  }

  async generatePdf(
    requestData: GeneratePdfRequest[]
  ): Promise<GeneratePdfResponse> {
    return this.httpService.post<GeneratePdfResponse>(
      `${this.baseUrl}/document/generate-pdf`,
      requestData
    );
  }

  // Coming soon...
  // async PCSSCriminalAppearanceDetails(
  //   fileId: string,
  //   appearanceId: string,
  //   partId: string,
  //   seqNo: string
  // ): Promise<CriminalAppearanceDetails> {
  //   //?partId=127956.0102&profSeqNo=31
  //   return this.httpService.get<any>(
  //     `${this.baseUrl}/pcss/criminal/${fileId}/appearance-detail/${appearanceId}/?partId=${partId}&profSeqNo=${seqNo}`
  //   );
  // }
}
