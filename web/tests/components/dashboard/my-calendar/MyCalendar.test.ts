import MyCalendar from '@/components/dashboard/my-calendar/MyCalendar.vue';
import { DashboardService } from '@/services';
import { flushPromises, mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/services');

describe('MyCalendar.vue', () => {
  let dashboardService: any;

  beforeEach(() => {
    dashboardService = {
      getMySchedule: vi.fn().mockResolvedValue({ payload: [] }),
    };

    (DashboardService as any).mockReturnValue(dashboardService);
  });

  const mountComponent = () => {
    return mount(MyCalendar, {
      props: {
        judgeId: 1,
        selectedDate: new Date(),
      },
      global: {
        provide: {
          dashboardService,
        },
      },
    });
  };

  it('renders MyCalendarToolbar and FullCalendar', async () => {
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
