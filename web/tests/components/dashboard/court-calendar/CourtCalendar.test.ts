import CourtCalendar from '@/components/dashboard/court-calendar/CourtCalendar.vue';
import { DashboardService } from '@/services';
import { CalendarViewEnum } from '@/types/common';
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

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
        selectedDate: new Date(),
        calendarView: CalendarViewEnum.TwoWeekView,
      },
      global: {
        provide: {
          dashboardService,
        },
      },
    });
  };

  it('renders CourtCalendarToolbar and FullCalendar', async () => {
    const wrapper = mountComponent();

    await flushPromises();

    expect(wrapper.find('.fc').exists()).toBeTruthy();
  });

  it('shows v-skeleton-loader briefly before FullCalendar', async () => {
    const wrapper = mountComponent();

    expect(wrapper.find('v-skeleton-loader').exists()).toBeTruthy();

    await flushPromises();

    expect(wrapper.find('.fc').exists()).toBeTruthy();
    expect(wrapper.find('v-skeleton-loader').exists()).toBeFalsy();
  });
});
