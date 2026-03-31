import CourtCalendarPresidersDay from '@/components/dashboard/court-calendar/CourtCalendarPresidersDay.vue';
import { useDarsStore } from '@/stores/DarsStore';
import { CalendarDayActivity } from '@/types';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { describe, expect, it, vi } from 'vitest';

const pinia = createPinia();
setActivePinia(pinia);

const createActivity = (
  overrides: Partial<CalendarDayActivity> = {}
): CalendarDayActivity => ({
  judgeId: faker.number.int(),
  judgeInitials: faker.string.alphanumeric(4),
  judgeName: faker.person.fullName(),
  locationName: faker.location.city(),
  locationShortName: faker.location.city(),
  locationId: faker.number.int({ min: 1, max: 999 }),
  period: faker.helpers.arrayElement(['AM', 'PM']),
  activityCode: faker.string.alphanumeric(4),
  activityDisplayCode: faker.lorem.word(),
  activityDescription: faker.lorem.sentence(),
  activityClassCode: faker.string.alpha(3),
  activityClassDescription: faker.lorem.word(),
  roomCode: faker.location.buildingNumber(),
  showDars: true,
  isJudgeAway: false,
  isJudgeBorrowed: false,
  isRemote: false,
  filesCount: 0,
  continuationsCount: 0,
  restrictions: [],
  ...overrides,
});

const mountComponent = (activities: CalendarDayActivity[], date?: Date) =>
  mount(CourtCalendarPresidersDay, {
    props: { activities, ...(date !== undefined ? { date } : {}) },
    global: {
      plugins: [pinia],
      stubs: {
        'v-tooltip': {
          name: 'VTooltip',
          inheritAttrs: false,
          props: ['text'],
          template:
            '<div v-bind="$attrs"><slot name="activator" :props="{}" /></div>',
        },
      },
    },
  });

describe('CourtCalendarPresidersDay.vue', () => {
  const mockShortName = faker.location.city();
  const mockPeriod = faker.helpers.arrayElement(['AM', 'PM']);
  const mockActivityDisplayCode = faker.lorem.word();
  const mockRoomCode = faker.location.buildingNumber();

  const mockActivities = [
    {
      judgeId: faker.number.int(),
      judgeInitials: faker.string.alphanumeric(4),
      locationShortName: mockShortName,
      period: mockPeriod,
      activityDisplayCode: mockActivityDisplayCode,
      roomCode: mockRoomCode,
      activityClassDescription: faker.lorem.word(),
      showDars: true,
      locationId: 1,
    } as CalendarDayActivity,
    {
      judgeId: faker.number.int(),
      judgeInitials: faker.string.alphanumeric(4),
      locationShortName: mockShortName,
      period: mockPeriod,
      activityDisplayCode: mockActivityDisplayCode,
      roomCode: mockRoomCode,
      activityClassDescription: faker.lorem.word(),
      showDars: true,
      locationId: 1,
    } as CalendarDayActivity,
    {
      judgeId: faker.number.int(),
      judgeInitials: faker.string.alphanumeric(4),
      locationShortName: mockShortName,
      period: mockPeriod,
      activityDisplayCode: mockActivityDisplayCode,
      roomCode: mockRoomCode,
      activityClassDescription: faker.lorem.word(),
      showDars: true,
      isJudgeAway: true,
      locationId: 1,
    } as CalendarDayActivity,
    {
      judgeId: faker.number.int(),
      judgeInitials: faker.string.alphanumeric(4),
      locationShortName: mockShortName,
      period: mockPeriod,
      activityDisplayCode: mockActivityDisplayCode,
      roomCode: mockRoomCode,
      activityClassDescription: faker.lorem.word(),
      showDars: true,
      isJudgeBorrowed: true,
      locationId: 1,
    } as CalendarDayActivity,
  ];

  it('renders CourtCalendarPresidersDay', async () => {
    const wrapper = mount(CourtCalendarPresidersDay, {
      props: {
        activities: mockActivities,
      },
      global: {
        plugins: [pinia],
      },
    });

    expect(wrapper.find('[data-testid="short-name"]').text()).toBe(
      mockShortName
    );

    expect(wrapper.find('[data-testid="judge-initials"]').exists()).toBe(true);
    expect(wrapper.findAll('[data-testid="dars"]')[0].exists()).toBe(true);

    const judgeActivities = wrapper.findAll('[data-testid="judge-activities"]');

    expect(
      judgeActivities.filter((e) => e.classes().includes('is-away')).length
    ).toBe(1);
    expect(
      judgeActivities.filter((e) => e.classes().includes('is-borrowed')).length
    ).toBe(1);
  });

  it('opens DARS modal with correct data when dars button is clicked', async () => {
    const mockDate = new Date('2023-10-15'); // Define the mock date for this test

    const wrapper = mount(CourtCalendarPresidersDay, {
      props: {
        activities: mockActivities,
        date: mockDate, // Pass consistent date prop
      },
      global: {
        plugins: [pinia],
      },
    });

    const darsStore = useDarsStore();

    // Simulate clicking the DARS button
    await wrapper.find('[data-testid="dars"]').trigger('click');

    // Check that the store state was updated correctly
    expect(darsStore.isModalVisible).toBe(true);
    expect(darsStore.searchLocationId).toBe(
      mockActivities[0].locationId?.toString()
    );
    expect(darsStore.searchRoom).toBe(mockActivities[0].roomCode);
    expect(darsStore.searchDate?.toISOString()).toEqual(mockDate.toISOString());
  });

  describe('empty activities', () => {
    it('renders nothing when activities is empty', () => {
      const wrapper = mountComponent([]);

      expect(wrapper.findAll('[data-testid="activity-detail"]')).toHaveLength(
        0
      );
      expect(wrapper.findAll('[data-testid="judge-activities"]')).toHaveLength(
        0
      );
    });
  });

  describe('showDars', () => {
    it('hides the DARS icon when showDars is false', () => {
      const wrapper = mountComponent([createActivity({ showDars: false })]);

      expect(wrapper.find('[data-testid="dars"]').exists()).toBe(false);
    });

    it('shows the DARS icon when showDars is true', () => {
      const wrapper = mountComponent([createActivity({ showDars: true })]);

      expect(wrapper.find('[data-testid="dars"]').exists()).toBe(true);
    });
  });

  describe('roomCode', () => {
    it('does not render the room span when roomCode is empty', () => {
      const wrapper = mountComponent([createActivity({ roomCode: '' })]);

      expect(wrapper.find('[data-testid="room"]').exists()).toBe(false);
    });

    it('renders the room span when roomCode is present', () => {
      const roomCode = '101';
      const wrapper = mountComponent([createActivity({ roomCode })]);

      expect(wrapper.find('[data-testid="room"]').text()).toBe(`(${roomCode})`);
    });
  });

  describe('activity grouping', () => {
    it('groups multiple activities under the same judge row', () => {
      const initials = 'AB';
      const locationShortName = faker.location.city();
      const a1 = createActivity({ judgeInitials: initials, locationShortName });
      const a2 = createActivity({ judgeInitials: initials, locationShortName });

      const wrapper = mountComponent([a1, a2]);

      expect(wrapper.findAll('[data-testid="judge-activities"]')).toHaveLength(
        1
      );
      expect(wrapper.findAll('[data-testid="display-code"]')).toHaveLength(2);
    });
  });

  describe('is-away / is-borrowed classes', () => {
    it('does not apply is-away when only some activities for a judge are away', () => {
      const initials = 'AB';
      const locationShortName = faker.location.city();
      const awayActivity = createActivity({
        judgeInitials: initials,
        locationShortName,
        isJudgeAway: true,
      });
      const nonAwayActivity = createActivity({
        judgeInitials: initials,
        locationShortName,
        isJudgeAway: false,
      });

      const wrapper = mountComponent([awayActivity, nonAwayActivity]);

      expect(
        wrapper.find('[data-testid="judge-activities"]').classes()
      ).not.toContain('is-away');
    });

    it('does not apply is-borrowed when only some activities for a judge are borrowed', () => {
      const initials = 'CD';
      const locationShortName = faker.location.city();
      const borrowedActivity = createActivity({
        judgeInitials: initials,
        locationShortName,
        isJudgeBorrowed: true,
      });
      const nonBorrowedActivity = createActivity({
        judgeInitials: initials,
        locationShortName,
        isJudgeBorrowed: false,
      });

      const wrapper = mountComponent([borrowedActivity, nonBorrowedActivity]);

      expect(
        wrapper.find('[data-testid="judge-activities"]').classes()
      ).not.toContain('is-borrowed');
    });
  });

  describe('judgeName tooltip', () => {
    it('passes judgeName as the text prop on the judge initials tooltip', () => {
      const judgeName = faker.person.fullName();
      const wrapper = mountComponent([createActivity({ judgeName })]);

      // First VTooltip is the judge-initials one; the second is the activity-description one
      expect(
        wrapper.findAllComponents({ name: 'VTooltip' })[0].props('text')
      ).toBe(judgeName);
    });
  });

  describe('openDarsModal edge cases', () => {
    it('stores null for searchLocationId when locationId is undefined', async () => {
      const localPinia = createPinia();
      setActivePinia(localPinia);

      const activity = createActivity({ locationId: undefined });
      const wrapper = mount(CourtCalendarPresidersDay, {
        props: { activities: [activity], date: new Date() },
        global: { plugins: [localPinia] },
      });

      await wrapper.find('[data-testid="dars"]').trigger('click');

      const darsStore = useDarsStore();
      expect(darsStore.searchLocationId).toBeNull();
    });

    it('uses the current date when no date prop is provided', async () => {
      const fakeNow = new Date('2024-06-15T10:00:00.000Z');
      vi.useFakeTimers();
      vi.setSystemTime(fakeNow);

      const localPinia = createPinia();
      setActivePinia(localPinia);

      const activity = createActivity();
      const wrapper = mount(CourtCalendarPresidersDay, {
        props: { activities: [activity] },
        global: { plugins: [localPinia] },
      });

      await wrapper.find('[data-testid="dars"]').trigger('click');

      const darsStore = useDarsStore();
      expect(darsStore.searchDate?.toISOString()).toBe(fakeNow.toISOString());

      vi.useRealTimers();
    });
  });
});
