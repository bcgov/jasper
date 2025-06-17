import { CalendarSchedule } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { HttpService } from './HttpService';

export class DashboardService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
  }

  async getMonthlySchedule(
    year: number,
    month: number,
    locationIds: string = ''
  ): Promise<CalendarSchedule> {
    return await this.httpService.get<CalendarSchedule>(
      `api/dashboard/monthly-schedule?year=${year}&month=${month}&locationIds=${locationIds}`
    );
  }

  async getMySchedule(
    startDate: string,
    endDate: string,
    todaysDate: string
  ): Promise<ApiResponse<CalendarSchedule>> {
    return await this.httpService.get<ApiResponse<CalendarSchedule>>(
      `api/dashboard/my-schedule?startDate=${startDate}&endDate=${endDate}&todaysDate=${todaysDate}`
    );
  }
}
