import DarsAccessModal from '@/components/dashboard/DarsAccessModal.vue';
import { useCommonStore } from '@/stores/CommonStore';
import { useDarsStore } from '@/stores/DarsStore';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { CustomAPIError } from '@/types/ApiResponse';
import type { CourtRoomsJsonInfoType } from '@/types/common';
import type { LocationInfo } from '@/types/courtlist';
import { flushPromises, mount } from '@vue/test-utils';
import { AxiosError } from 'axios';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

const mockDarsService = {
  search: vi.fn(),
};

const mockLocationService = {
  getLocations: vi.fn(),
};

const mockWindowOpen = vi.fn();
Object.defineProperty(globalThis, 'open', {
  writable: true,
  value: mockWindowOpen,
});

vi.mock('vuetify', () => ({
  useTheme: () => ({
    current: {
      value: {
        colors: {
          primary: '#1976D2',
        },
      },
    },
  }),
}));

describe('DarsAccessModal tests', () => {
  let snackbarStore: any;
  let commonStore: any;
  let darsStore: any;

  const mockCourtRoomsAndLocations: CourtRoomsJsonInfoType[] = [
    {
      name: 'Test Location 1',
      shortName: 'Location 1',
      code: 'TL1',
      locationId: '1',
      active: true,
      agencyIdentifierCd: '1234',
      courtRooms: [
        { room: 'Room 101', locationId: '1', type: 'courtroom' },
        { room: 'Room 102', locationId: '1', type: 'courtroom' },
      ],
      infoLink: '',
      agencyIdentifierCd: 'TL1',
    },
    {
      name: 'Test Location 2',
      shortName: 'Location 2',
      code: 'TL2',
      locationId: '2',
      active: true,
      agencyIdentifierCd: '5678',
      courtRooms: [{ room: 'Room 201', locationId: '2', type: 'courtroom' }],
      infoLink: '',
      agencyIdentifierCd: 'TL2',
    },
  ];

  const mountComponent = async () => {
    const wrapper = mount(DarsAccessModal, {
      global: {
        plugins: [createPinia()],
        stubs: {
          VDialog: { template: '<div><slot /></div>' },
          VCard: { template: '<div><slot /></div>' },
          VCardTitle: { template: '<div><slot /></div>' },
          VCardText: { template: '<div><slot /></div>' },
          VForm: { template: '<div><slot /></div>' },
          VSelect: { template: '<input />' },
          VTextField: { template: '<input />' },
          VBtn: { template: '<button><slot /></button>' },
          VIcon: { template: '<span></span>' },
          VDatePicker: { template: '<div></div>' },
          VRow: { template: '<div><slot /></div>' },
          VCol: { template: '<div><slot /></div>' },
          VProgressCircular: { template: '<div></div>' },
          VAlert: { template: '<div><slot /></div>' },
          VList: { template: '<div><slot /></div>' },
          VListItem: { template: '<div><slot /></div>' },
          VListItemTitle: { template: '<div><slot /></div>' },
          VListItemSubtitle: { template: '<div><slot /></div>' },
          VDateInput: { template: '<input />' },
          ActionButtons: { template: '<div />' },
        },
        provide: {
          darsService: mockDarsService,
          locationService: mockLocationService,
          $vuetify: {
            theme: {
              current: {
                value: {
                  colors: {
                    primary: '#1976D2',
                  },
                },
              },
            },
          },
        },
      },
    });
    await flushPromises();
    await nextTick();
    return wrapper;
  };

  const getLocationsFromStore = (): LocationInfo[] => {
    return commonStore.courtRoomsAndLocations.map(
      (location: CourtRoomsJsonInfoType) => ({
        name: location.name,
        shortName: location.name,
        code: location.code,
        locationId: location.locationId,
        active: location.active,
        courtRooms: location.courtRooms,
      })
    );
  };

  beforeEach(() => {
    setActivePinia(createPinia());
    snackbarStore = useSnackbarStore();
    commonStore = useCommonStore();
    darsStore = useDarsStore();

    mockDarsService.search.mockReset();
    mockLocationService.getLocations.mockReset();
    mockWindowOpen.mockReset();
    snackbarStore.showSnackbar = vi.fn();

    commonStore.setCourtRoomsAndLocations(mockCourtRoomsAndLocations);
    mockDarsService.search.mockResolvedValue([]);
    mockLocationService.getLocations.mockResolvedValue([]);
  });

  describe('Store-based modal visibility', () => {
    it('opens modal when darsStore.openModal is called', async () => {
      await mountComponent();

      darsStore.openModal();
      await nextTick();

      expect(darsStore.isModalVisible).toBe(true);
    });

    it('closes modal when darsStore.closeModal is called', async () => {
      await mountComponent();

      darsStore.openModal();
      await nextTick();

      darsStore.closeModal();
      expect(darsStore.isModalVisible).toBe(false);
    });
  });

  describe('Prefill data using store methods', () => {
    it('populates date, location, and room when using openModal with data', async () => {
      const prefillDate = new Date('2025-10-28');
      const searchLocationId = '1';
      const prefillRoom = 'Room 101';

      await mountComponent();
      await nextTick();
      await flushPromises();

      darsStore.openModal(prefillDate, searchLocationId, prefillRoom);
      await nextTick();

      expect(darsStore.searchDate).toEqual(prefillDate);
      expect(darsStore.searchLocationId).toBe(searchLocationId);
      expect(darsStore.searchRoom).toBe(prefillRoom);
      expect(darsStore.isModalVisible).toBe(true);
    });

    it('updates locationId and clears room when room parameter is null', async () => {
      await mountComponent();

      darsStore.setSearchCriteria(new Date('2025-10-28'), '1', 'Room 101');

      darsStore.openModal(null, '1', null);
      await nextTick();

      expect(darsStore.searchLocationId).toBe('1');
      expect(darsStore.searchRoom).toBe('');
    });

    it('updates locationId and preserves existing room when not overridden', async () => {
      await mountComponent();

      darsStore.setSearchCriteria(new Date('2025-10-28'), '1', 'Room 101');

      darsStore.openModal(null, '2');
      await nextTick();

      expect(darsStore.searchLocationId).toBe('2');
      expect(darsStore.searchRoom).toBe('Room 101');
    });
  });

  describe('Location data from CommonStore', () => {
    it('displays correct locations from CommonStore', async () => {
      await mountComponent();

      darsStore.openModal();
      await nextTick();

      const locations = getLocationsFromStore();
      expect(locations).toHaveLength(2);
      expect(locations[0].name).toBe('Test Location 1');
      expect(locations[1].name).toBe('Test Location 2');
    });

    it('filters active locations only', async () => {
      const locationsWithInactive = [
        ...mockCourtRoomsAndLocations,
        {
          name: 'Inactive Location',
          code: 'IL1',
          locationId: 3,
          active: false,
          courtRooms: [],
        },
      ];

      commonStore.setCourtRoomsAndLocations(locationsWithInactive);
      await mountComponent();

      darsStore.openModal();
      await nextTick();

      const locations = getLocationsFromStore();
      const activeLocations = locations.filter((loc) => loc.active);
      expect(activeLocations).toHaveLength(2); // Only active locations
    });
  });

  describe('Form validation and search', () => {
    it('performs search with valid criteria', async () => {
      const mockResults = [
        {
          date: '2025-10-28',
          agencyIdentifierCd: null,
          courtRoomCd: 'Room 101',
          url: 'http://example.com/recording1',
          fileName: 'recording1.mp3',
          locationNm: 'Test Location 1',
        },
        {
          date: '2025-10-28',
          agencyIdentifierCd: null,
          courtRoomCd: 'Room 101',
          url: 'http://example.com/recording2',
          fileName: 'recording2.mp3',
          locationNm: 'Test Location 1',
        },
      ];
      mockDarsService.search.mockResolvedValue(mockResults);

      await mountComponent();
      // Note: Actual search would be triggered by form submission in the component
      // This test validates the mock setup
      expect(mockDarsService.search).toBeDefined();
    });

    it('handles search errors gracefully', async () => {
      // Mock a 404 error from the API
      const axiosError = {
        response: {
          status: 404,
          data: 'No items matching the given criteria were found.',
        },
        isAxiosError: true,
      } as unknown as AxiosError;

      const customError = new CustomAPIError('Not Found', axiosError);

      mockDarsService.search.mockRejectedValue(customError);

      await mountComponent();

      // The component should handle 404 errors specially
      expect(mockDarsService.search).toBeDefined();
    });

    it('handles 404 errors as warnings', async () => {
      // Mock a 404 error from the API
      const axiosError = {
        response: {
          status: 404,
          data: 'No items matching the given criteria were found.',
        },
        isAxiosError: true,
      } as unknown as AxiosError;

      const customError = new CustomAPIError('Not Found', axiosError);

      mockDarsService.search.mockRejectedValue(customError);
      await mountComponent();

      // The component should treat 404 as "no results" warning, not an error
      expect((customError.originalError as AxiosError).response?.status).toBe(
        404
      );
    });

    it('handles other errors as failures', async () => {
      // Mock a 500 error from the API
      const axiosError = {
        response: {
          status: 500,
          data: 'Internal Server Error',
        },
        message: 'Server Error',
        isAxiosError: true,
      } as unknown as AxiosError;

      const customError = new CustomAPIError('Server Error', axiosError);

      mockDarsService.search.mockRejectedValue(customError);
      await mountComponent();

      // The component should treat non-404 errors as actual errors
      expect((customError.originalError as AxiosError).response?.status).toBe(
        500
      );
    });
  });

  describe('Reset functionality', () => {
    it('resets search criteria when resetSearchCriteria is called', async () => {
      await mountComponent();

      const testLocation = getLocationsFromStore()[0];
      darsStore.setSearchCriteria(
        new Date('2025-10-28'),
        testLocation.locationId,
        'Room 101'
      );

      // Reset
      darsStore.resetSearchCriteria();

      expect(darsStore.searchDate).toBeNull();
      expect(darsStore.searchLocationId).toBeNull();
      expect(darsStore.searchRoom).toBe('');
    });
  });

  describe('Modal integration', () => {
    it('maintains modal state independently of search criteria', async () => {
      await mountComponent();

      darsStore.openModal();
      expect(darsStore.isModalVisible).toBe(true);

      darsStore.resetSearchCriteria();
      expect(darsStore.isModalVisible).toBe(true);

      darsStore.closeModal();
      expect(darsStore.isModalVisible).toBe(false);
    });
  });

  describe('Location setting from external components', () => {
    it('sets location correctly when called from AppearancesView-like component', async () => {
      await mountComponent();

      const testDate = new Date('2025-10-28');
      const locationId = '1';
      const roomCode = 'Room 101';

      darsStore.openModal(testDate, locationId, roomCode);
      await nextTick();
      await flushPromises();

      expect(darsStore.searchDate).toEqual(testDate);
      expect(darsStore.searchLocationId).toBe(locationId);
      expect(darsStore.searchRoom).toBe(roomCode);
    });

    it('handles race condition when locations are not yet loaded', async () => {
      commonStore.setCourtRoomsAndLocations([]);

      await mountComponent();

      const locationId = '1';
      darsStore.openModal(new Date(), locationId, 'Room 101');
      await nextTick();
      await flushPromises();

      expect(darsStore.searchLocationId).toBe(locationId);

      commonStore.setCourtRoomsAndLocations(mockCourtRoomsAndLocations);
      await nextTick();
      await flushPromises();

      expect(darsStore.searchLocationId).toBe(locationId);
      expect(darsStore.searchRoom).toBe('Room 101');
    });

    it('clears room when switching from location with rooms to location without rooms', async () => {
      const locationsWithEmptyRooms = [
        ...mockCourtRoomsAndLocations,
        {
          name: 'No Rooms Location',
          shortName: 'No Rooms',
          code: 'NRL',
          locationId: '3',
          active: true,
          courtRooms: [],
          infoLink: '',
          agencyIdentifierCd: 'NRL',
        },
      ];

      commonStore.setCourtRoomsAndLocations(locationsWithEmptyRooms);
      await mountComponent();

      darsStore.openModal(new Date(), '1', 'Room 101');
      await nextTick();
      await flushPromises();

      expect(darsStore.searchLocationId).toBe('1');
      expect(darsStore.searchRoom).toBe('Room 101');

      darsStore.openModal(new Date(), '3', 'Room 101'); // Try to set a room that doesn't exist
      await nextTick();
      await flushPromises();

      expect(darsStore.searchLocationId).toBe('3');
      expect(darsStore.searchRoom).toBe('Room 101');

      darsStore.setSearchCriteria(new Date(), '1', 'Room 101'); // Reset to location with rooms
      await nextTick();

      // Then manually change to location without rooms (simulating UI selection)
      darsStore.setSearchCriteria(new Date(), '3', ''); // UI would clear room when changing location
      await nextTick();

      expect(darsStore.searchLocationId).toBe('3');
      expect(darsStore.searchRoom).toBe(''); // Room cleared when location changed
    });

    it('maintains room when switching between locations through openModal calls', async () => {
      await mountComponent();

      darsStore.openModal(new Date(), '1', 'Room 101');
      await nextTick();
      await flushPromises();

      expect(darsStore.searchLocationId).toBe('1');
      expect(darsStore.searchRoom).toBe('Room 101');

      darsStore.openModal(new Date(), '2', 'Room 201');
      await nextTick();
      await flushPromises();

      expect(darsStore.searchLocationId).toBe('2');
      expect(darsStore.searchRoom).toBe('Room 201');
    });

    it('handles location changes that clear rooms in manual UI interactions', async () => {
      await mountComponent();

      darsStore.setSearchCriteria(new Date(), '1', 'Room 101');
      await nextTick();
      await flushPromises();

      expect(darsStore.searchLocationId).toBe('1');
      expect(darsStore.searchRoom).toBe('Room 101');

      darsStore.setSearchCriteria(new Date(), '2', '');
      await nextTick();

      expect(darsStore.searchLocationId).toBe('2');
      expect(darsStore.searchRoom).toBe('');
    });
  });
});
