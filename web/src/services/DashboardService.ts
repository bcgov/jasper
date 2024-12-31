import { CalendarSchedule } from '@/types';
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
}
