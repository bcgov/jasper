import { LocationService } from '@/services';
import { HttpService } from '@/services/HttpService';
import { useCommonStore, useCourtListStore } from '@/stores';
import { mount } from '@vue/test-utils';
import CourtListSearch from 'CMP/courtlist/CourtListSearch.vue';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/stores');
vi.mock('@/services/HttpService');
vi.mock('@/services');

describe('CourtListSearch.vue', () => {
  let wrapper: any;
  let commonStore: any;
  let courtListStore: any;
  let locationService: any;
  let httpService: any;

  beforeEach(() => {
    commonStore = {
      updateCourtRoomsAndLocations: vi.fn(),
    };
    courtListStore = {
      courtListInformation: {
        detailsData: null,
      },
    };
    locationService = {
      getLocations: vi.fn().mockResolvedValue([
        {
          locationId: 1,
          name: 'Location 1',
          courtRooms: [{ room: 'Room 1' }],
        },
      ]),
    };
    httpService = {
      get: vi.fn().mockResolvedValue({}),
    };

    (useCommonStore as any).mockReturnValue(commonStore);
    (useCourtListStore as any).mockReturnValue(courtListStore);
    (LocationService as any).mockReturnValue(locationService);
    (HttpService as any).mockReturnValue(httpService);

    wrapper = mount(CourtListSearch, {
      global: {
        provide: {
          httpService,
          locationService,
        },
      },
    });
  });

  it('renders correctly', () => {
    expect(wrapper.exists()).toBe(true);
  });

  it('fetches court locations on mount', async () => {
    expect(locationService.getLocations).toHaveBeenCalled();
    expect(commonStore.updateCourtRoomsAndLocations).toHaveBeenCalledWith([
      { locationId: 1, name: 'Location 1', courtRooms: [{ room: 'Room 1' }] },
    ]);
  });

  it('should pass form validation', async () => {
    wrapper.vm.selectedCourtLocation = {
      locationId: 1,
      name: 'Location 1',
      courtRooms: [{ room: 'Room 1' }],
    };
    wrapper.vm.selectedCourtRoom = 'Room 1';
    expect(wrapper.vm.validateForm()).toBe(true);
  });

  it('should pass form validation when viewing own court-list', async () => {
    wrapper.vm.selectedCourtLocation = null;
    wrapper.vm.selectedCourtRoom = null;
    wrapper.vm.schedule = 'my_schedule';
    expect(wrapper.vm.validateForm()).toBe(true);
  });

  it('should fail form validation', async () => {
    wrapper.vm.selectedCourtLocation = null;
    wrapper.vm.selectedCourtRoom = null;
    wrapper.vm.schedule = 'room_schedule';
    expect(wrapper.vm.validateForm()).toBe(false);
    expect(wrapper.vm.errors.isMissingLocation).toBe(true);
    expect(wrapper.vm.errors.isMissingRoom).toBe(true);
  });

  it('searches for court list', async () => {
    wrapper.vm.selectedCourtLocation = {
      locationId: 1,
      name: 'Location 1',
      courtRooms: [{ room: 'Room 1' }],
    };
    wrapper.vm.selectedCourtRoom = 'Room 1';
    wrapper.vm.date = new Date('2023-01-01');
    wrapper.vm.schedule = 'room_schedule';

    await wrapper.vm.searchForCourtList();
    expect(httpService.get).toHaveBeenCalledWith(
      'api/courtlist/court-list?agencyId=1&roomCode=Room 1&proceeding=2023-01-01'
    );
    await nextTick();

    expect(courtListStore.courtListInformation.detailsData).toEqual({});
  });

  it('emits courtListSearched event', async () => {
    wrapper.vm.selectedCourtLocation = {
      locationId: 1,
      name: 'Location 1',
      courtRooms: [{ room: 'Room 1' }],
    };
    wrapper.vm.selectedCourtRoom = 'Room 1';
    wrapper.vm.date = new Date('2023-01-01');
    wrapper.vm.schedule = 'my_schedule';

    await wrapper.vm.searchForCourtList();
    await nextTick();

    expect(wrapper.emitted().courtListSearched).toBeTruthy();
  });
});
