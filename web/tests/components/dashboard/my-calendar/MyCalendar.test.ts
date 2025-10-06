import MyCalendar from '@/components/dashboard/my-calendar/MyCalendar.vue';
import { DashboardService } from '@/services';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

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
