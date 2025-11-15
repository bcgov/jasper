import MyCalendarDayExpanded from '@/components/dashboard/my-calendar/MyCalendarDayExpanded.vue';
import {
  AdjudicatorRestriction,
  CalendarDay,
  CalendarDayActivity,
} from '@/types';
import { formatDateInstanceToDDMMMYYYY } from '@/utils/dateUtils';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it, vi } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useDarsStore } from '@/stores/DarsStore';

const pinia = createPinia();
setActivePinia(pinia);

describe('MyCalendarDayExpanded.vue', () => {
  it('renders all fields in the expanded panel', () => {
    const date = faker.date.anytime();
    const formattedDate = formatDateInstanceToDDMMMYYYY(date);
    const mockLocation = faker.location.city();
    const mockActivityDescription = faker.lorem.sentence();
    const mockRoomCode = faker.location.buildingNumber();
    const mockRestrictions = [
      {
        isCivil: false,
        fileId: faker.string.alphanumeric(),
        fileName: faker.system.fileName(),
        appearanceReasonCode: faker.string.alphanumeric(),
      },
      {
        isCivil: true,
        fileId: faker.string.alphanumeric(),
        fileName: faker.system.fileName(),
        appearanceReasonCode: faker.string.alphanumeric(),
      },
    ] as AdjudicatorRestriction[];

    const wrapper = mount(MyCalendarDayExpanded, {
      global: {
        plugins: [pinia],
        stubs: {
          teleport: true, // Disable the Teleport behavior
        },
      },
      props: {
        expandedDate: formattedDate,
        day: {
          date: formattedDate,
          showCourtList: true,
          activities: [
            {
              locationName: mockLocation,
              isRemote: true,
              period: 'AM',
              activityClassDescription: faker.lorem.word(),
              activityDescription: mockActivityDescription,
              roomCode: mockRoomCode,
              restrictions: mockRestrictions,
              showDars: true,
            } as CalendarDayActivity,
          ] as CalendarDayActivity[],
        } as CalendarDay,
        close: vi.fn(),
      },
    });

    const courtListEl = wrapper.find('[data-testid="court-list"]');
    const locationRemoteIconEl = wrapper.find(
      '[data-testid="location-remote-icon"]'
    );
    const locationEl = wrapper.find('[data-testid="name"]');
    const periodEl = wrapper.find('[data-testid="period"]');
    const activityEl = wrapper.find('[data-testid="activity"]');
    const roomEl = wrapper.find('[data-testid="room"]');
    const darsEl = wrapper.find('[data-testid="dars"]');
    const restrictionsEl = wrapper.find('[data-testid="restrictions"]');

    expect(courtListEl.exists()).toBeTruthy();
    expect(locationRemoteIconEl.exists()).toBeTruthy();
    expect(locationEl.text()).toBe(mockLocation);
    expect(periodEl.text()).toBe('AM');
    expect(activityEl.text()).toBe(mockActivityDescription);
    expect(darsEl.exists()).toBe(true);
    expect(roomEl.text()).toBe(`(${mockRoomCode})`);
    expect(restrictionsEl.exists()).toBe(true);

    const links = restrictionsEl.findAll('a');

    expect(links.length).toBe(mockRestrictions.length);

    expect(links[0].attributes('href')).toBe(
      `/criminal-file/${mockRestrictions[0].fileId}`
    );
    expect(links[0].text()).toBe(
      `${mockRestrictions[0].fileName} (${mockRestrictions[0].appearanceReasonCode})`
    );
    expect(links[1].attributes('href')).toBe(
      `/civil-file/${mockRestrictions[1].fileId}`
    );
    expect(links[1].text()).toBe(
      `${mockRestrictions[1].fileName} (${mockRestrictions[1].appearanceReasonCode})`
    );
  });

  it('renders bare minimum fields in the expanded panel', () => {
    const date = faker.date.anytime();
    const formattedDate = formatDateInstanceToDDMMMYYYY(date);
    const mockLocation = faker.location.city();
    const mockActivityDescription = faker.lorem.sentence();

    const wrapper = mount(MyCalendarDayExpanded, {
      global: {
        plugins: [pinia],
        stubs: {
          teleport: true, // Disable the Teleport behavior
        },
      },
      props: {
        expandedDate: formattedDate,
        day: {
          date: formattedDate,
          showCourtList: false,
          activities: [
            {
              locationName: mockLocation,
              isRemote: false,
              activityClassDescription: faker.lorem.word(),
              activityDescription: mockActivityDescription,
              showDars: false,
            } as CalendarDayActivity,
          ] as CalendarDayActivity[],
        } as CalendarDay,
        close: vi.fn(),
      },
    });

    const courtListEl = wrapper.find('[data-testid="court-list"]');
    const locationRemoteIconEl = wrapper.find(
      '[data-testid="location-remote-icon"]'
    );
    const locationEl = wrapper.find('[data-testid="name"]');
    const periodEl = wrapper.find('[data-testid="period"]');
    const activityEl = wrapper.find('[data-testid="activity"]');
    const roomEl = wrapper.find('[data-testid="room"]');
    const darsEl = wrapper.find('[data-testid="dars"]');
    const restrictionsEl = wrapper.find('[data-testid="restrictions"]');

    expect(courtListEl.exists()).toBe(false);
    expect(locationRemoteIconEl.exists()).toBe(false);
    expect(locationEl.text()).toBe(mockLocation);
    expect(periodEl.exists()).toBe(false);
    expect(activityEl.text()).toBe(mockActivityDescription);
    expect(darsEl.exists()).toBe(false);
    expect(roomEl.exists()).toBe(false);
    expect(restrictionsEl.exists()).toBe(false);
  });

  it('opens DARS modal with correct data when dars button is clicked', async () => {
    const date = faker.date.anytime();
    const formattedDate = formatDateInstanceToDDMMMYYYY(date);
    const mockLocation = faker.location.city();
    const mockRoomCode = faker.location.buildingNumber();

    const wrapper = mount(MyCalendarDayExpanded, {
      global: {
        plugins: [pinia],
        stubs: {
          teleport: true,
        },
      },
      props: {
        expandedDate: formattedDate,
        day: {
          date: formattedDate,
          showCourtList: true,
          activities: [
            {
              locationName: mockLocation,
              roomCode: mockRoomCode,
              showDars: true,
            } as CalendarDayActivity,
          ] as CalendarDayActivity[],
        } as CalendarDay,
        close: vi.fn(),
      },
    });

    const darsStore = useDarsStore();

    await wrapper.find('[data-testid="dars"]').trigger('click');

    expect(darsStore.isModalVisible).toBe(true);
    const midnightDate = new Date(date.toDateString());
    expect(darsStore.searchDate?.toISOString()).toEqual(
      midnightDate.toISOString()
    );
    expect(darsStore.searchLocationId).toBe(null);
    expect(darsStore.searchRoom).toBe(mockRoomCode);
  });
});
