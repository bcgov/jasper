import { ApiResponse } from '@/types/ApiResponse';
import { AppearanceDocumentRequest, DocumentBundleResponse } from '@/types/common';
import { HttpService } from './HttpService';

export class BundleService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }


  async generateBundle(
    appearances: AppearanceDocumentRequest[]
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    return this.httpService.post<ApiResponse<DocumentBundleResponse>>(
      `api/binders/bundle`,
      appearances
    );
  }
}