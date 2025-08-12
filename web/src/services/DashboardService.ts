import { CalendarDay, PersonSearchItem } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { CourtCalendarSchedule } from '@/types/CourtCalendarSchedule';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class DashboardService extends ServiceBase {
  constructor(httpService: IHttpService) {
    super(httpService);
  }

  getCourtCalendar(
    startDate: string,
    endDate: string,
    locationIds: string
  ): Promise<ApiResponse<CourtCalendarSchedule>> {
    return this.httpService.get<ApiResponse<CourtCalendarSchedule>>(
      `api/dashboard/court-calendar?startDate=${startDate}&endDate=${endDate}&locationIds=${locationIds}`
    );
  }

  getMySchedule(
    startDate: string,
    endDate: string,
    judgeId: number | undefined
  ): Promise<ApiResponse<CalendarDay[]>> {
    return this.httpService.get<ApiResponse<CalendarDay[]>>(
      `api/dashboard/my-schedule?startDate=${startDate}&endDate=${endDate}&judgeId=${judgeId ?? ''}`
    );
  }

  getTodaysSchedule(
    judgeId: number | undefined
  ): Promise<ApiResponse<CalendarDay>> {
    return this.httpService.get<ApiResponse<CalendarDay>>(
      `api/dashboard/today?judgeId=${judgeId ?? ''} `
    );
  }

  getJudges(): Promise<PersonSearchItem[]> {
    return this.httpService.get<PersonSearchItem[]>(
      `api/dashboard/judges`,
      {},
      { skipErrorHandler: true }
    );
  }
}
