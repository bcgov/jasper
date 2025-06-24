import MyCalendarDay from '@/components/dashboard/MyCalendarDay.vue';
import { CalendarDayActivity } from '@/types';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('MyCalendarDay.test.ts', () => {
  it('renders 1 activity details', () => {
    const mockShortName = faker.location.city();
    const mockPeriod = faker.helpers.arrayElement(['AM', 'PM']);
    const mockActivityDisplayCode = faker.lorem.word();
    const mockRoomCode = faker.location.buildingNumber();

    const mockActivities = [
      {
        locationShortName: mockShortName,
        period: mockPeriod,
        activityDisplayCode: mockActivityDisplayCode,
        roomCode: mockRoomCode,
        activityClassDescription: faker.lorem.word(),
        isRemote: true,
      } as CalendarDayActivity,
    ];

    const wrapper = mount(MyCalendarDay, {
      props: {
        activities: mockActivities,
      },
    });

    const shortNameEl = wrapper.find('[data-testid="short-name"]');
    const activityEl = wrapper.find('[data-testid="activity"]');
    const roomEl = wrapper.find('[data-testid="room"]');
    const periodEl = wrapper.find('v-chip');
    const remoteEl = wrapper.find('v-icon');

    expect(shortNameEl).not.toBeNull();
    expect(shortNameEl.text()).toBe(mockShortName);

    expect(activityEl).not.toBeNull();
    expect(activityEl.text()).toBe(mockActivityDisplayCode);

    expect(roomEl).not.toBeNull();
    expect(roomEl.text()).toBe(`(${mockRoomCode})`);

    expect(periodEl).not.toBeNull();
    expect(periodEl.text()).toBe(mockPeriod.toString());

    expect(remoteEl).not.toBeNull();
  });

  it('renders 2 activity details', () => {
    const mockShortName = faker.location.city();
    const mockPeriod = faker.helpers.arrayElement(['AM', 'PM']);
    const mockActivityDisplayCode = faker.lorem.word();
    const mockRoomCode = faker.location.buildingNumber();

    const mockActivities = [
      {
        locationShortName: mockShortName,
        period: mockPeriod,
        activityDisplayCode: mockActivityDisplayCode,
        roomCode: mockRoomCode,
        activityClassDescription: faker.lorem.word(),
        isRemote: true,
      } as CalendarDayActivity,
      {
        locationShortName: mockShortName,
        period: mockPeriod,
        activityDisplayCode: mockActivityDisplayCode,
        roomCode: mockRoomCode,
        activityClassDescription: faker.lorem.word(),
        isRemote: true,
      } as CalendarDayActivity,
    ];

    const wrapper = mount(MyCalendarDay, {
      props: {
        activities: mockActivities,
      },
    });

    const activities = wrapper.findAll('[data-testid="activity-detail"]');

    expect(activities.length).toBe(mockActivities.length);
  });

  it('renders available activity details', () => {
    const mockActivityDescription = faker.lorem.word();

    const mockActivities = [
      {
        activityClassDescription: faker.lorem.word(),
        activityDescription: mockActivityDescription,
        isRemote: false,
      } as CalendarDayActivity,
    ];

    const wrapper = mount(MyCalendarDay, {
      props: {
        activities: mockActivities,
      },
    });

    const activityEl = wrapper.find('[data-testid="activity"]');
    const roomEl = wrapper.find('[data-testid="room"]');
    const periodEl = wrapper.find('v-chip');
    const remoteEl = wrapper.find('v-icon');

    expect(activityEl).not.toBeNull();
    expect(activityEl.text()).toBe(mockActivityDescription);

    expect(roomEl.exists()).toBeFalsy();
    expect(periodEl.exists()).toBeFalsy();
    expect(remoteEl.exists()).toBeFalsy();
  });
});
