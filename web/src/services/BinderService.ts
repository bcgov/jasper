import { Binder } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';
import { CriminalDocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { DocumentBundleResponse } from '@/types/DocumentBundleResponse';

type BinderBundleContexts = Record<string, string>[];
type BinderBundleRequest = CriminalDocumentBundleRequest | BinderBundleContexts;

export class BinderService extends ServiceBase {
  constructor(httpService: IHttpService) {
    super(httpService);
  }

  getBinders(
    queryParams: Record<string, any> | undefined
  ): Promise<ApiResponse<Binder[]>> {
    return this.httpService.get<ApiResponse<Binder[]>>(
      `api/binders`,
      queryParams,
      { skipErrorHandler: true }
    );
  }

  addBinder(binder: Binder): Promise<ApiResponse<Binder>> {
    return this.httpService.post<ApiResponse<Binder>>(
      `api/binders`,
      binder,
      {},
      'json',
      {
        skipErrorHandler: true,
      }
    );
  }

  updateBinder(binder: Binder): Promise<ApiResponse<Binder>> {
    return this.httpService.put<ApiResponse<Binder>>(
      `api/binders`,
      binder,
      {},
      'json',
      {
        skipErrorHandler: true,
      }
    );
  }

  deleteBinder(binderId: string): Promise<void> {
    return this.httpService.delete(`api/binders/${binderId}`, {}, 'json', {
      skipErrorHandler: true,
    });
  }

  async viewBinderPDF(
    bundleRequest: BinderBundleRequest,
    categories: string[]
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    const categoryParams = categories.length
      ? '?category=' + categories.join(',')
      : '';
    const url = 'api/binders/bundle/view' + categoryParams;

    return this.httpService.post<ApiResponse<DocumentBundleResponse>>(
      url,
      bundleRequest
    );
  }

  async generateBinderPDF(
    bundleRequest: BinderBundleRequest,
    categories: string[]
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    const categoryParams = categories.length
      ? '?category=' + categories.join(',')
      : '';

    const url = 'api/binders/bundle' + categoryParams;

    const body = Array.isArray(bundleRequest)
      ? bundleRequest
      : bundleRequest.appearances;

    return this.httpService.post<ApiResponse<DocumentBundleResponse>>(
      url,
      body
    );
  }
}
