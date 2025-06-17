import CourtToday from '@/components/dashboard/CourtToday.vue';
import { CalendarDayActivity, CalendarDayV2 } from '@/types';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('CourtToday.vue', () => {
  it(`renders 'activityDisplayCode' when 'activityDescription' is null`, () => {
    const mockActivity = faker.lorem.word();
    const mockToday = {
      activities: [
        {
          activityClassDescription: faker.lorem.word(),
          activityDisplayCode: mockActivity,
        } as CalendarDayActivity,
      ],
    } as CalendarDayV2;

    const wrapper = mount(CourtToday, {
      props: {
        today: mockToday,
      },
    });

    const activitiesComps = wrapper.findAll('v-slide-group-item');
    expect(activitiesComps.length).toBe(1);

    const noCourtEl = activitiesComps[0].find(
      '[data-testid="no-court-scheduled"]'
    );
    expect(noCourtEl.text()).toBe(mockActivity);
  });

  it(`renders 1 activity`, () => {
    const mockLocation = faker.location.city();
    const mockToday = {
      activities: [
        {
          locationName: mockLocation,
          period: 'AM',
          activityDescription: faker.lorem.word(),
          activityClassDescription: faker.lorem.word(),
          filesCount: 1,
          continuationsCount: 1,
        } as CalendarDayActivity,
      ],
    } as CalendarDayV2;

    const wrapper = mount(CourtToday, {
      props: {
        today: mockToday,
      },
    });

    const activitiesComps = wrapper.findAll('v-slide-group-item');
    expect(activitiesComps.length).toBe(1);

    const locationEl = activitiesComps[0].find('h2');
    expect(locationEl.text()).toBe(`${mockLocation} (AM)`);

    const scheduledEl = activitiesComps[0].find('[data-testid="scheduled"]');
    expect(scheduledEl.text()).toBe(`Scheduled:1 file(1 continuation)`);
  });

  it(`renders multiple activities`, () => {
    const mockLocation = faker.location.city();
    const mockToday = {
      activities: [
        {
          locationName: mockLocation,
          period: 'AM',
          activityDescription: faker.lorem.word(),
          activityClassDescription: faker.lorem.word(),
          filesCount: 2,
          continuationsCount: 2,
        } as CalendarDayActivity,
        {
          locationName: mockLocation,
          period: 'AM',
          activityDescription: faker.lorem.word(),
          activityClassDescription: faker.lorem.word(),
          filesCount: 1,
          continuationsCount: 1,
        } as CalendarDayActivity,
      ],
    } as CalendarDayV2;

    const wrapper = mount(CourtToday, {
      props: {
        today: mockToday,
      },
    });

    const activitiesComps = wrapper.findAll('v-slide-group-item');
    expect(activitiesComps.length).toBe(2);

    const scheduledEl = activitiesComps[0].find('[data-testid="scheduled"]');
    expect(scheduledEl.text()).toBe(`Scheduled:2 files(2 continuations)`);
  });
});
