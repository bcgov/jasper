import { Binder } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { ServiceBase } from './ServiceBase';

export class BinderService extends ServiceBase {
  async getBinders(
    queryParams: Record<string, any> | undefined
  ): Promise<ApiResponse<Binder[]>> {
    return await this.httpService.get<ApiResponse<Binder[]>>(
      `api/binders`,
      queryParams
    );
  }

  async addBinder(binder: Binder): Promise<ApiResponse<Binder>> {
    return await this.httpService.post<ApiResponse<Binder>>(
      `api/binders`,
      binder
    );
  }

  async updateBinder(binder: Binder): Promise<ApiResponse<Binder>> {
    return await this.httpService.put<ApiResponse<Binder>>(
      `api/binders`,
      binder
    );
  }

  async deleteBinder(binderId: string): Promise<void> {
    await this.httpService.delete(`api/binders/${binderId}`);
  }
}
