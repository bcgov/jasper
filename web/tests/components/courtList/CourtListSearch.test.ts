import { CourtListService, LocationService } from '@/services';
import { useCommonStore, useCourtListStore } from '@/stores';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { mount } from '@vue/test-utils';
import CourtListSearch from 'CMP/courtlist/CourtListSearch.vue';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/stores');
vi.mock('@/services');

describe('CourtListSearch.vue', () => {
  const TEN_MINUTES = 600000;
  const NINE_MINUTES = 540000;
  const ONE_MINUTE = 60000;

  let snackbarStore: ReturnType<typeof useSnackbarStore>;
  let wrapper: any;
  let commonStore: any;
  let courtListStore: any;
  let locationService: any;
  let courtListService: any;

  beforeEach(() => {
    vi.useFakeTimers();
    setActivePinia(createPinia());
    snackbarStore = useSnackbarStore();
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
    courtListService = {
      getCourtList: vi.fn().mockResolvedValue(Promise.resolve({})),
      getMyCourtList: vi.fn().mockResolvedValue(Promise.resolve({})),
    };

    (useCommonStore as any).mockReturnValue(commonStore);
    (useCourtListStore as any).mockReturnValue(courtListStore);
    (LocationService as any).mockReturnValue(locationService);
    (CourtListService as any).mockReturnValue(courtListService);

    wrapper = mount(CourtListSearch, {
      global: {
        provide: {
          courtListService,
          locationService,
        },
      },
    });
  });

  afterEach(() => {
    vi.useRealTimers();
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
    expect(courtListService.getCourtList).toHaveBeenCalledWith(
      '1',
      'Room 1',
      '2023-01-01',
      undefined
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

  it('shows warning before auto-refresh', async () => {
    wrapper.vm.selectedCourtLocation = {
      locationId: 1,
      name: 'Location 1',
      courtRooms: [{ room: 'Room 1' }],
    };
    wrapper.vm.schedule = 'my_schedule';
    wrapper.vm.appliedDate = new Date();

    await vi.advanceTimersByTimeAsync(NINE_MINUTES);

    expect(snackbarStore.isVisible).toBe(true);
  });

  it('hides warning after auto-refresh', async () => {
    wrapper.vm.selectedCourtLocation = {
      locationId: 1,
      name: 'Location 1',
      courtRooms: [{ room: 'Room 1' }],
    };
    wrapper.vm.schedule = 'my_schedule';
    wrapper.vm.appliedDate = new Date();

    await vi.advanceTimersByTimeAsync(NINE_MINUTES);
    expect(snackbarStore.isVisible).toBe(true);
    await vi.advanceTimersByTimeAsync(ONE_MINUTE);
    expect(snackbarStore.isVisible).toBe(false);
  });

  it(`searches for new court-list data after ${TEN_MINUTES} ms`, async () => {
    wrapper.vm.selectedCourtLocation = {
      locationId: 1,
      name: 'Location 1',
      courtRooms: [{ room: 'Room 1' }],
    };
    wrapper.vm.schedule = 'my_schedule';
    wrapper.vm.appliedDate = new Date('2023-01-01');
    wrapper.vm.date = new Date('2023-01-01');
    wrapper.vm.selectedCourtRoom = 'Room 1';

    await vi.advanceTimersByTimeAsync(TEN_MINUTES);

    expect(courtListService.getCourtList).toHaveBeenCalledWith(
      '1',
      'Room 1',
      '2023-01-01',
      undefined
    );
  });
});
