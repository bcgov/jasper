import MyCalendarDay from '@/components/dashboard/my-calendar/MyCalendarDay.vue';
import { AdjudicatorRestriction, CalendarDayActivity } from '@/types';
import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
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
        restrictions: [{} as AdjudicatorRestriction],
      } as CalendarDayActivity,
    ];

    const wrapper = mount(MyCalendarDay, {
      props: {
        date: formatDateInstanceToDDMMMYYYY(new Date()),
        activities: mockActivities,
        isWeekend: false,
      },
    });

    const shortNameEl = wrapper.find('[data-testid="short-name"]');
    const activityEl = wrapper.find('[data-testid="activity"]');
    const roomEl = wrapper.find('[data-testid="room"]');
    const periodEl = wrapper.find('v-chip');
    const remoteEl = wrapper.find('v-icon');
    const arEl = wrapper.find('[data-testid="activity-restrictions"]');

    expect(shortNameEl).not.toBeNull();
    expect(shortNameEl.text()).toBe(mockShortName);

    expect(activityEl).not.toBeNull();
    expect(activityEl.text()).toBe(mockActivityDisplayCode);

    expect(roomEl).not.toBeNull();
    expect(roomEl.text()).toBe(`(${mockRoomCode})`);

    expect(periodEl).not.toBeNull();
    expect(periodEl.text()).toBe(mockPeriod.toString());

    expect(remoteEl).not.toBeNull();

    expect(arEl.text()).toBe('1');
  });

  it('renders 1 location with multiple activities when location is the same', () => {
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
        date: formatDateInstanceToDDMMMYYYY(new Date()),

        activities: mockActivities,
        isWeekend: false,
      },
    });

    const locations = wrapper.findAll('[data-testid="short-name"]');
    const activities = wrapper.findAll('[data-testid="activity"]');
    const locationRemoteIconEl = wrapper.find(
      '[data-testid="location-remote-icon"]'
    );

    expect(locations.length).toBe(1);
    expect(activities.length).toBe(2);
    expect(locationRemoteIconEl.exists()).toBeTruthy();
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
        date: formatDateInstanceToDDMMMYYYY(new Date()),
        activities: mockActivities,
        isWeekend: false,
      },
    });

    const activityEl = wrapper.find('[data-testid="activity"]');
    const roomEl = wrapper.find('[data-testid="room"]');
    const periodEl = wrapper.find('v-chip');
    const remoteEl = wrapper.find('v-icon');
    const locationRemoteIconEl = wrapper.find(
      '[data-testid="location-remote-icon"]'
    );

    expect(activityEl).not.toBeNull();
    expect(activityEl.text()).toBe(mockActivityDescription);

    expect(roomEl.exists()).toBeFalsy();
    expect(periodEl.exists()).toBeFalsy();
    expect(remoteEl.exists()).toBeFalsy();
    expect(locationRemoteIconEl.exists()).toBeFalsy();
  });

  it('renders "Weekend" when date falls on a weekend', () => {
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
        date: formatDateInstanceToDDMMMYYYY(new Date()),

        activities: mockActivities,
        isWeekend: true,
      },
    });

    const activityEl = wrapper.find('[data-testid="activity"]');

    expect(activityEl).not.toBeNull();
    expect(activityEl.text()).toBe('Weekend');
  });

  it('renders multiple location with multiple activities', () => {
    const mockShortName1 = faker.location.city();
    const mockShortName2 = faker.location.city();
    const mockPeriod = faker.helpers.arrayElement(['AM', 'PM']);
    const mockActivityDisplayCode = faker.lorem.word();
    const mockRoomCode = faker.location.buildingNumber();

    const mockActivities = [
      {
        locationShortName: mockShortName1,
        period: mockPeriod,
        activityDisplayCode: mockActivityDisplayCode,
        roomCode: mockRoomCode,
        activityClassDescription: faker.lorem.word(),
        isRemote: true,
      } as CalendarDayActivity,
      {
        locationShortName: mockShortName1,
        period: mockPeriod,
        activityDisplayCode: mockActivityDisplayCode,
        roomCode: mockRoomCode,
        activityClassDescription: faker.lorem.word(),
        isRemote: false,
      } as CalendarDayActivity,
      {
        locationShortName: mockShortName2,
        period: mockPeriod,
        activityDisplayCode: mockActivityDisplayCode,
        roomCode: mockRoomCode,
        activityClassDescription: faker.lorem.word(),
        isRemote: true,
      } as CalendarDayActivity,
    ];

    const wrapper = mount(MyCalendarDay, {
      props: {
        date: formatDateInstanceToDDMMMYYYY(new Date()),
        activities: mockActivities,
        isWeekend: false,
      },
    });

    const activityDetails = wrapper.findAll('[data-testid="activity-detail"]');
    const locationRemoteIcons = wrapper.findAll(
      '[data-testid="location-remote-icon"]'
    );
    const activityRemoteIcons = wrapper.findAll(
      '[data-testid="activity-remote-icon"]'
    );

    expect(activityDetails.length).toBe(2);
    expect(locationRemoteIcons.length).toBe(1);
    expect(activityRemoteIcons.length).toBe(1);
  });
});
