import CourtCalendarDay from '@/components/dashboard/court-calendar/CourtCalendarDay.vue';
import DarsAccessModal from '@/components/dashboard/DarsAccessModal.vue';
import { CalendarDayActivity } from '@/types';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';

const pinia = createPinia();
setActivePinia(pinia);

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

  it('displays DarsAccessModal with correct data when dars button is clicked', async () => {
    const mockDate = new Date();

    const wrapper = mount(CourtCalendarDay, {
      props: {
        activities: mockActivities,
        date: mockDate, // Pass consistent date prop
      },
      global: {
        stubs: {
          DarsAccessModal: false, // Remove stub to test actual behavior
        },
      },
    });

    // Simulate clicking the DARS button
    await wrapper.find('[data-testid="dars"]').trigger('click');

    // Find the modal within the wrapper
    const modalWrapper = wrapper.findComponent(DarsAccessModal);

    expect(modalWrapper.exists()).toBe(true); // Ensure the modal is rendered
    expect(modalWrapper.props('modelValue')).toBe(true);
    expect(modalWrapper.props('prefillLocationId')).toBe(
      mockActivities[0].locationId
    );
    expect(modalWrapper.props('prefillRoom')).toBe(mockActivities[0].roomCode);

    const prefillDate = modalWrapper.props('prefillDate');
    expect(prefillDate?.toISOString()).toEqual(mockDate.toISOString());
  });
});
