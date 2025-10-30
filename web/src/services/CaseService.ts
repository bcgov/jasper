import { AssignedCaseResponse } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class CaseService extends ServiceBase {
  constructor(httpService: IHttpService) {
    super(httpService);
  }

  getAssignedCases(
    queryParams: Record<string, any> | undefined
  ): Promise<ApiResponse<AssignedCaseResponse>> {
    return this.httpService.get<ApiResponse<AssignedCaseResponse>>(
      `api/cases`,
      queryParams
    );
  }
}
