import CourtToday from '@/components/dashboard/CourtToday.vue';
import { DashboardService } from '@/services';
import { CalendarDay, CalendarDayActivity } from '@/types';
import { faker } from '@faker-js/faker';
import { flushPromises, mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';

vi.mock('@/services');

describe('CourtToday.vue', () => {
  let dashboardService: any;

  const setupDashboardMock = (response: {
    succeeded: boolean;
    payload: CalendarDay;
  }) => {
    dashboardService = {
      getTodaysSchedule: vi.fn().mockResolvedValue(response),
    };

    (DashboardService as any).mockReturnValue(dashboardService);
  };

  const mountComponent = (props: any) => {
    return mount(CourtToday, {
      props: {
        ...props,
      },
      global: {
        provide: {
          dashboardService,
        },
      },
    });
  };

  it(`renders 'activityDescription' when 'activityDisplayCode' is null`, async () => {
    const mockActivity = faker.lorem.word();
    const response = {
      succeeded: true,
      payload: {
        activities: [
          {
            activityClassDescription: faker.lorem.word(),
            activityDescription: mockActivity,
          } as CalendarDayActivity,
        ],
      } as CalendarDay,
    };

    setupDashboardMock(response);
    const wrapper = mountComponent({});

    await flushPromises();

    const activitiesComps = wrapper.findAll('v-slide-group-item');
    expect(activitiesComps.length).toBe(1);

    const noCourtEl = activitiesComps[0].find(
      '[data-testid="no-court-scheduled"]'
    );
    expect(noCourtEl.text()).toBe(mockActivity);
  });

  it(`renders 1 activity`, async () => {
    const mockLocation = faker.location.city();
    const response = {
      succeeded: true,
      payload: {
        activities: [
          {
            locationName: mockLocation,
            period: 'AM',
            activityDisplayCode: faker.lorem.word(),
            activityDescription: faker.lorem.word(),
            activityClassDescription: faker.lorem.word(),
            filesCount: 1,
            continuationsCount: 1,
          } as CalendarDayActivity,
        ],
      } as CalendarDay,
    };

    setupDashboardMock(response);
    const wrapper = mountComponent({});

    await flushPromises();

    const activitiesComps = wrapper.findAll('v-slide-group-item');
    expect(activitiesComps.length).toBe(1);

    const locationEl = activitiesComps[0].find('h2');
    expect(locationEl.text()).toBe(`${mockLocation} (AM)`);

    const scheduledEl = activitiesComps[0].find('[data-testid="scheduled"]');
    expect(scheduledEl.text()).toBe(`Scheduled:1 file(1 continuation)`);
  });

  it(`renders multiple activities`, async () => {
    const mockLocation = faker.location.city();
    const response = {
      succeeded: true,
      payload: {
        activities: [
          {
            locationName: mockLocation,
            period: 'AM',
            activityDescription: faker.lorem.word(),
            activityDisplayCode: faker.lorem.word(),

            activityClassDescription: faker.lorem.word(),
            filesCount: 2,
            continuationsCount: 2,
          } as CalendarDayActivity,
          {
            locationName: mockLocation,
            period: 'AM',
            activityDescription: faker.lorem.word(),
            activityDisplayCode: faker.lorem.word(),
            activityClassDescription: faker.lorem.word(),
            filesCount: 1,
            continuationsCount: 1,
          } as CalendarDayActivity,
        ],
      } as CalendarDay,
    };

    setupDashboardMock(response);
    const wrapper = mountComponent({});

    await flushPromises();

    const activitiesComps = wrapper.findAll('v-slide-group-item');
    expect(activitiesComps.length).toBe(2);

    const scheduledEl = activitiesComps[0].find('[data-testid="scheduled"]');
    expect(scheduledEl.text()).toBe(`Scheduled:2 files(2 continuations)`);
  });

  it('shows v-skeleton-loader briefly before the court activities', async () => {
    const wrapper = mountComponent({});

    expect(wrapper.find('v-skeleton-loader').exists()).toBeTruthy();

    await flushPromises();

    expect(wrapper.find('v-skeleton-loader').exists()).toBeFalsy();
  });
});
