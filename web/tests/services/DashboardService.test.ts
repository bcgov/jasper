import { DashboardService } from '@/services/DashboardService';
import { IHttpService } from '@/services/HttpService';
import { ApiResponse } from '@/types/ApiResponse';
import { CalendarDay } from '@/types/CalendarDay';
import { CourtCalendarActivitiesSchedule } from '@/types/CourtCalendarActivitiesSchedule';
import { CourtCalendarPresidersSchedule } from '@/types/CourtCalendarPresidersSchedule';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';

const mockHttpService = {
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  delete: vi.fn(),
} as unknown as IHttpService;

class TestableDashboardService extends DashboardService {
  constructor() {
    super(mockHttpService);
  }
}

describe('DashboardService', () => {
  let service: DashboardService;

  beforeEach(() => {
    vi.clearAllMocks();
    service = new TestableDashboardService();
  });

  describe('getCourtCalendarPresiders', () => {
    const mockResponse: ApiResponse<CourtCalendarPresidersSchedule> = {
      data: { days: [], activities: [], presiders: [] },
    } as unknown as ApiResponse<CourtCalendarPresidersSchedule>;

    it('should call the correct URL with all parameters when judgeId is provided', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      const result = await service.getCourtCalendarPresiders(
        '2026-03-01',
        '2026-03-31',
        '101,102',
        42
      );

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/court-calendar/presiders?startDate=2026-03-01&endDate=2026-03-31&locationIds=101,102&judgeId=42'
      );
      expect(result).toEqual(mockResponse);
    });

    it('should use empty string for judgeId when undefined', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      await service.getCourtCalendarPresiders(
        '2026-03-01',
        '2026-03-31',
        '101',
        undefined
      );

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/court-calendar/presiders?startDate=2026-03-01&endDate=2026-03-31&locationIds=101&judgeId='
      );
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(
        service.getCourtCalendarPresiders('2026-03-01', '2026-03-31', '101', 1)
      ).rejects.toThrow('Network error');
    });
  });

  describe('getCourtCalendarActivities', () => {
    const mockResponse: ApiResponse<CourtCalendarActivitiesSchedule> = {
      data: { days: [], activities: [] },
    } as unknown as ApiResponse<CourtCalendarActivitiesSchedule>;

    it('should call the correct URL with all parameters', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      const result = await service.getCourtCalendarActivities(
        '2026-03-01',
        '2026-03-31',
        '101,102'
      );

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/court-calendar/activities?startDate=2026-03-01&endDate=2026-03-31&locationIds=101,102'
      );
      expect(result).toEqual(mockResponse);
    });

    it('should handle an empty locationIds string', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      await service.getCourtCalendarActivities('2026-03-01', '2026-03-31', '');

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/court-calendar/activities?startDate=2026-03-01&endDate=2026-03-31&locationIds='
      );
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(
        service.getCourtCalendarActivities('2026-03-01', '2026-03-31', '101')
      ).rejects.toThrow('Network error');
    });
  });

  describe('getMySchedule', () => {
    const mockCalendarDays: CalendarDay[] = [
      {
        date: '2026-03-01',
        isWeekend: false,
        showCourtList: true,
        activities: [],
      },
    ];
    const mockResponse = { data: mockCalendarDays } as unknown as ApiResponse<
      CalendarDay[]
    >;

    it('should call the correct URL with judgeId when provided', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      const result = await service.getMySchedule('2026-03-01', '2026-03-31', 7);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/my-schedule?startDate=2026-03-01&endDate=2026-03-31&judgeId=7'
      );
      expect(result).toEqual(mockResponse);
    });

    it('should use empty string for judgeId when undefined', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      await service.getMySchedule('2026-03-01', '2026-03-31', undefined);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/my-schedule?startDate=2026-03-01&endDate=2026-03-31&judgeId='
      );
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(
        service.getMySchedule('2026-03-01', '2026-03-31', 1)
      ).rejects.toThrow('Network error');
    });
  });

  describe('getTodaysSchedule', () => {
    const mockCalendarDay: CalendarDay = {
      date: '2026-03-31',
      isWeekend: false,
      showCourtList: true,
      activities: [],
    };
    const mockResponse = {
      data: mockCalendarDay,
    } as unknown as ApiResponse<CalendarDay>;

    it('should call the correct URL with judgeId when provided', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      const result = await service.getTodaysSchedule(5);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/today?judgeId=5 '
      );
      expect(result).toEqual(mockResponse);
    });

    it('should use empty string for judgeId when undefined', async () => {
      (mockHttpService.get as Mock).mockResolvedValueOnce(mockResponse);

      await service.getTodaysSchedule(undefined);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        'api/dashboard/today?judgeId= '
      );
    });

    it('should handle API errors', async () => {
      const error = new Error('Network error');
      (mockHttpService.get as Mock).mockRejectedValueOnce(error);

      await expect(service.getTodaysSchedule(1)).rejects.toThrow(
        'Network error'
      );
    });
  });
});
