import CourtCalendar from '@/components/dashboard/court-calendar/CourtCalendar.vue';
import { DashboardService } from '@/services';
import { CalendarViewEnum } from '@/types/common';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/services');

describe('CourtCalendar.vue', () => {
  let dashboardService: any;

  beforeEach(() => {
    dashboardService = {
      getCourtCalendar: vi.fn().mockResolvedValue({
        payload: { days: [], presiders: [], activities: [] },
      }),
    };

    (DashboardService as any).mockReturnValue(dashboardService);
  });

  const mountComponent = () => {
    return mount(CourtCalendar, {
      props: {
        judgeId: 1,
        selectedDate: new Date(),
        calendarView: CalendarViewEnum.TwoWeekView,
        isCalendarLoading: true,
      },
      global: {
        provide: {
          dashboardService,
        },
      },
    });
  };

  it('renders skeleton loader when isCalendarLoading is true while FullCalendar is hidden', async () => {
    const wrapper: any = mountComponent();

    expect(wrapper.find('v-skeleton-loader').exists()).toBeTruthy();
    expect(wrapper.find('.fc').exists()).toBeFalsy();
  });

  it('renders FullCalendar when isCalendarLoading is false while skeleton loader is hidden', async () => {
    const wrapper: any = mountComponent();

    expect(wrapper.find('v-skeleton-loader').exists()).toBeTruthy();
    expect(wrapper.find('.fc').exists()).toBeFalsy();

    wrapper.vm.isCalendarLoading = false;
    await nextTick();

    expect(wrapper.find('v-skeleton-loader').exists()).toBeFalsy();
    expect(wrapper.find('.fc').exists()).toBeTruthy();
  });
});
