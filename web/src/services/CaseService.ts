import { AssignedCaseResponse, Case } from '@/types';
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

  setDueDateOverride(
    item: Case,
    overrideDueDate: string | null
  ): Promise<ApiResponse<Case[]>> {
    return this.httpService.put<ApiResponse<Case[]>>(
      `api/cases/${encodeURIComponent(item.id)}/due-date-override`,
      { overrideDueDate, updatedDate: item.updatedDate },
      {},
      'json',
      { suppressConflictLogout: true, suppressErrorNotification: true }
    );
  }
}
