import CourtCalendarDay from '@/components/dashboard/court-calendar/CourtCalendarDay.vue';
import { CalendarDayActivity } from '@/types';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

describe('CourtCalendarDay.vue', () => {
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
    } as CalendarDayActivity,
  ];

  it('renders CourtCalendarDay', () => {
    const wrapper = mount(CourtCalendarDay, {
      props: {
        activities: mockActivities,
      },
    });

    expect(wrapper.find('[data-testid="short-name"]').text()).toBe(
      mockShortName
    );
    expect(wrapper.findAll('[data-testid="judge-initials"]')[0].text()).toBe(
      mockActivities[0].judgeInitials
    );
    expect(wrapper.findAll('[data-testid="dars"]')[0].exists()).toBe(true);

    const judgeActivities = wrapper.findAll('[data-testid="judge-activities"]');

    expect(
      judgeActivities.filter((e) => e.classes().includes('is-away')).length
    ).toBe(1);
    expect(
      judgeActivities.filter((e) => e.classes().includes('is-borrowed')).length
    ).toBe(1);
  });
});
