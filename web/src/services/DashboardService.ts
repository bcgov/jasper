import { CalendarDay, CourtCalendarActivitiesSchedule } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { CourtCalendarPresidersSchedule } from '@/types/CourtCalendarPresidersSchedule';
import { IHttpService } from './HttpService';
import { ServiceBase } from './ServiceBase';

export class DashboardService extends ServiceBase {
  constructor(httpService: IHttpService) {
    super(httpService);
  }

  getCourtCalendarPresiders(
    startDate: string,
    endDate: string,
    locationIds: string,
    judgeId: number | undefined
  ): Promise<ApiResponse<CourtCalendarPresidersSchedule>> {
    return this.httpService.get<ApiResponse<CourtCalendarPresidersSchedule>>(
      `api/dashboard/court-calendar/presiders?startDate=${startDate}&endDate=${endDate}&locationIds=${locationIds}&judgeId=${judgeId ?? ''}`
    );
  }

  getCourtCalendarActivities(
    startDate: string,
    endDate: string,
    locationIds: string
  ): Promise<ApiResponse<CourtCalendarActivitiesSchedule>> {
    return this.httpService.get<ApiResponse<CourtCalendarActivitiesSchedule>>(
      `api/dashboard/court-calendar/activities?startDate=${startDate}&endDate=${endDate}&locationIds=${locationIds}`
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
}
