import { CalendarDay } from '@/types';
import { ApiResponse } from '@/types/ApiResponse';
import { CourtCalendarSchedule } from '@/types/CourtCalendarSchedule';
import { HttpService } from './HttpService';

export class DashboardService {
  private readonly httpService: HttpService;

  constructor(httpService: HttpService) {
    this.httpService = httpService;
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
}
