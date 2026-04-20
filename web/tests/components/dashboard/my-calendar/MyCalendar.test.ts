import MyCalendar from '@/components/dashboard/my-calendar/MyCalendar.vue';
import { DashboardService } from '@/services';
import {
  AdjudicatorRestriction,
  CalendarDay,
  CalendarDayActivity,
} from '@/types';
import { faker } from '@faker-js/faker';
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/services');

describe('MyCalendar.vue', () => {
  let dashboardService: any;

  const createMockActivity = (
    overrides?: Partial<CalendarDayActivity>
  ): CalendarDayActivity => ({
    locationId: faker.number.int(),
    locationName: faker.location.city(),
    locationShortName: faker.string.alpha({ length: 3, casing: 'upper' }),
    activityCode: faker.string.alpha({ length: 3 }),
    activityDisplayCode: faker.lorem.word(),
    activityDescription: faker.lorem.sentence(),
    activityClassCode: 'SIT',
    activityClassDescription: 'Sitting',
    isRemote: false,
    roomCode: faker.location.buildingNumber(),
    period: 'AM',
    filesCount: 1,
    continuationsCount: 0,
    showDars: false,
    restrictions: [],
    judgeId: faker.number.int(),
    judgeName: faker.person.fullName(),
    judgeInitials: faker.string.alpha({ length: 2, casing: 'upper' }),
    isJudgeBorrowed: false,
    isJudgeAway: false,
    ...overrides,
  });

  const createMockCalendarDay = (
    overrides?: Partial<CalendarDay>
  ): CalendarDay => ({
    date: '15 Jan 2026',
    isWeekend: false,
    showCourtList: false,
    activities: [],
    ...overrides,
  });

  beforeEach(() => {
    dashboardService = {
      getMySchedule: vi.fn().mockResolvedValue({ payload: [] }),
    };

    (DashboardService as any).mockReturnValue(dashboardService);
  });

  const mountComponent = (props = {}) => {
    return mount(MyCalendar, {
      props: {
        judgeId: faker.number.int({ min: 1, max: 100 }),
        selectedDate: new Date(2026, 0, 15),
        isCalendarLoading: true,
        ...props,
      },
      global: {
        provide: {
          dashboardService,
        },
        stubs: {
          MyCalendarDayExpanded: true,
        },
      },
    });
  };

  describe('Loading state', () => {
    it('renders skeleton loader when isCalendarLoading is true while FullCalendar is hidden', () => {
      const wrapper = mountComponent({ isCalendarLoading: true });

      expect(wrapper.find('v-skeleton-loader').exists()).toBe(true);
      expect(wrapper.find('.fc').exists()).toBe(false);
    });

    it('renders FullCalendar when isCalendarLoading is false while skeleton loader is hidden', async () => {
      const wrapper = mountComponent({ isCalendarLoading: false });
      await flushPromises();

      expect(wrapper.find('v-skeleton-loader').exists()).toBe(false);
      expect(wrapper.find('.fc').exists()).toBe(true);
    });

    it('emits isCalendarLoading as false after data finishes loading', async () => {
      const wrapper = mountComponent({ isCalendarLoading: true });
      await flushPromises();

      const emitted = wrapper.emitted('update:isCalendarLoading');
      expect(emitted).toBeTruthy();

      const lastEmit = emitted![emitted!.length - 1];
      expect(lastEmit).toEqual([false]);
    });

    it('emits isCalendarLoading as false even when the service call fails', async () => {
      const consoleErrorSpy = vi
        .spyOn(console, 'error')
        .mockImplementation(() => {});
      dashboardService.getMySchedule = vi
        .fn()
        .mockRejectedValue(new Error('Network error'));

      const wrapper = mountComponent({ isCalendarLoading: true });
      await flushPromises();

      const emitted = wrapper.emitted('update:isCalendarLoading');
      expect(emitted).toBeTruthy();

      const lastEmit = emitted![emitted!.length - 1];
      expect(lastEmit).toEqual([false]);

      consoleErrorSpy.mockRestore();
    });
  });

  describe('Service calls', () => {
    it('calls getMySchedule on mount', async () => {
      mountComponent();

      await vi.waitFor(() => {
        expect(dashboardService.getMySchedule).toHaveBeenCalled();
      });
    });

    it('calls getMySchedule with the first and last day of the selected month', async () => {
      const testJudgeId = faker.number.int({ min: 1, max: 100 });
      mountComponent({
        judgeId: testJudgeId,
        selectedDate: new Date(2026, 0, 15),
      });

      await vi.waitFor(() => {
        expect(dashboardService.getMySchedule).toHaveBeenCalledWith(
          '01-Jan-2026',
          '31-Jan-2026',
          testJudgeId
        );
      });
    });

    it('logs an error when getMySchedule rejects', async () => {
      const error = new Error('Service unavailable');
      const consoleErrorSpy = vi
        .spyOn(console, 'error')
        .mockImplementation(() => {});
      dashboardService.getMySchedule = vi.fn().mockRejectedValue(error);

      mountComponent();
      await flushPromises();

      expect(consoleErrorSpy).toHaveBeenCalledWith(
        'Failed to load calendar data:',
        error
      );
      consoleErrorSpy.mockRestore();
    });
  });

  describe('Watchers', () => {
    it('reloads calendar data when judgeId prop changes', async () => {
      const newJudgeId = faker.number.int({ min: 51, max: 100 });
      const wrapper = mountComponent({ judgeId: 1 });

      await flushPromises();
      dashboardService.getMySchedule.mockClear();

      await wrapper.setProps({ judgeId: newJudgeId });
      await flushPromises();

      expect(dashboardService.getMySchedule).toHaveBeenCalledWith(
        expect.any(String),
        expect.any(String),
        newJudgeId
      );
    });

    it('reloads calendar data and updates the date range when selectedDate changes to a different month', async () => {
      const testJudgeId = faker.number.int({ min: 1, max: 100 });
      const wrapper = mountComponent({
        judgeId: testJudgeId,
        selectedDate: new Date(2026, 0, 15),
      });

      await flushPromises();
      dashboardService.getMySchedule.mockClear();

      await wrapper.setProps({ selectedDate: new Date(2026, 1, 1) });
      await flushPromises();

      expect(dashboardService.getMySchedule).toHaveBeenCalledWith(
        '01-Feb-2026',
        '28-Feb-2026',
        testJudgeId
      );
    });
  });

  describe('Expanded day panels', () => {
    it('renders MyCalendarDayExpanded for days with non-standard activity classes', async () => {
      const activity = createMockActivity({ activityClassCode: 'TRI' });
      const day = createMockCalendarDay({
        date: '15 Jan 2026',
        activities: [activity],
      });
      dashboardService.getMySchedule = vi
        .fn()
        .mockResolvedValue({ payload: [day] });

      const wrapper = mountComponent({ isCalendarLoading: false });
      await flushPromises();
      await nextTick();

      expect(
        wrapper.findAllComponents({ name: 'MyCalendarDayExpanded' }).length
      ).toBe(1);
    });

    it('renders MyCalendarDayExpanded for days where a Sitting activity has restrictions', async () => {
      const restriction = {
        pk: '1',
        judgeName: 'Smith',
        appearanceReasonCode: 'AR',
        fileName: 'file.pdf',
        fileId: '123',
        activityCode: 'ACT',
        restrictionCode: 'RC',
        roomCode: '101',
        isCivil: true,
      } as AdjudicatorRestriction;
      const activity = createMockActivity({
        activityClassCode: 'SIT',
        restrictions: [restriction],
      });
      const day = createMockCalendarDay({
        date: '15 Jan 2026',
        activities: [activity],
      });
      dashboardService.getMySchedule = vi
        .fn()
        .mockResolvedValue({ payload: [day] });

      const wrapper = mountComponent({ isCalendarLoading: false });
      await flushPromises();
      await nextTick();

      expect(
        wrapper.findAllComponents({ name: 'MyCalendarDayExpanded' }).length
      ).toBe(1);
    });

    it('does not render MyCalendarDayExpanded for days with only Sitting or NonSitting activities and no restrictions', async () => {
      const days = [
        createMockCalendarDay({
          date: '10 Jan 2026',
          activities: [
            createMockActivity({ activityClassCode: 'SIT', restrictions: [] }),
          ],
        }),
        createMockCalendarDay({
          date: '11 Jan 2026',
          activities: [
            createMockActivity({ activityClassCode: 'NS', restrictions: [] }),
          ],
        }),
      ];
      dashboardService.getMySchedule = vi
        .fn()
        .mockResolvedValue({ payload: days });

      const wrapper = mountComponent({ isCalendarLoading: false });
      await flushPromises();
      await nextTick();

      expect(
        wrapper.findAllComponents({ name: 'MyCalendarDayExpanded' }).length
      ).toBe(0);
    });

    it('renders one MyCalendarDayExpanded per day that has expandable activities', async () => {
      const expandableActivity = createMockActivity({
        activityClassCode: 'TRI',
      });
      const days = [
        createMockCalendarDay({
          date: '10 Jan 2026',
          activities: [expandableActivity],
        }),
        createMockCalendarDay({
          date: '11 Jan 2026',
          activities: [expandableActivity],
        }),
        createMockCalendarDay({ date: '12 Jan 2026', activities: [] }),
      ];
      dashboardService.getMySchedule = vi
        .fn()
        .mockResolvedValue({ payload: days });

      const wrapper = mountComponent({ isCalendarLoading: false });
      await flushPromises();
      await nextTick();

      expect(
        wrapper.findAllComponents({ name: 'MyCalendarDayExpanded' }).length
      ).toBe(2);
    });
  });
});
