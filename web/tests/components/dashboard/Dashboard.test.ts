import Dashboard from '@/components/dashboard/Dashboard.vue';
import { useCommonStore } from '@/stores';
import { mount, shallowMount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/stores');

describe('Dashboard.vue', () => {
  let commonStore: any;

  beforeEach(() => {
    commonStore = {
      userInfo: { judgeId: 1 },
    };
    (useCommonStore as any).mockReturnValue(commonStore);
  });

  const mountComponent = () => {
    return shallowMount(Dashboard, {
      global: {
        stubs: {
          CourtToday: true,
          CalendarToolbar: true,
          CourtCalendar: true,
          MyCalendar: true,
          DashboardPanels: true,
        },
      },
    });
  };

  it('renders CourtToday, MyCalendar and Panels as the default view for Dashboard component', () => {
    const wrapper = mountComponent();

    expect(wrapper.find('court-today-stub').exists()).toBe(true);
    expect(wrapper.find('calendar-toolbar-stub').exists()).toBe(true);
    expect(wrapper.find('my-calendar-stub').exists()).toBe(true);
    expect(wrapper.find('dashboard-panels-stub').exists()).toBe(true);
    expect(wrapper.find('court-calendar-stub').exists()).toBe(false);
  });

  it('renders CourtCalendar when flag is set', async () => {
    const wrapper = mountComponent();

    (wrapper.vm as any).isCourtCalendar = true;

    await nextTick();

    expect(wrapper.find('court-today-stub').exists()).toBe(false);
    expect(wrapper.find('calendar-toolbar-stub').exists()).toBe(true);
    expect(wrapper.find('my-calendar-stub').exists()).toBe(false);
    expect(wrapper.find('court-calendar-stub').exists()).toBe(true);
  });
});
