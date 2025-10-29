import DarsAccessModal from '@/components/dashboard/DarsAccessModal.vue';
import { useDarsStore } from '@/stores/DarsStore';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import type { DarsLogNote } from '@/services/DarsService';
import type { LocationInfo } from '@/types/courtlist';
import { flushPromises, mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

const mockDarsSearch = vi.fn();
const mockGetLocations = vi.fn();

const mockDarsService = {
  search: mockDarsSearch,
};

const mockLocationService = {
  getLocations: mockGetLocations,
};

const mockWindowOpen = vi.fn();
globalThis.window.open = mockWindowOpen;

// Helper to mount component with provide
const mountComponent = async (props = {}) => {
  const wrapper = mount(DarsAccessModal, {
    props: {
      modelValue: false,
      ...props,
    },
    global: {
      provide: {
        darsService: mockDarsService,
        locationService: mockLocationService,
      },
      stubs: {
        'v-form': {
          template: '<form @submit.prevent="$attrs.onSubmit"><slot /></form>',
          methods: {
            validate: () => Promise.resolve({ valid: true }),
            resetValidation: () => {},
          },
        },
      },
    },
  });
  await flushPromises();
  await nextTick();
  return wrapper;
};

describe('DarsAccessModal tests', () => {
  let snackbarStore: ReturnType<typeof useSnackbarStore>;
  let darsStore: ReturnType<typeof useDarsStore>;

  const mockLocations: LocationInfo[] = [
    {
      name: 'Vancouver',
      shortName: 'VAN',
      code: 'VAN',
      locationId: '1',
      courtRooms: [
        { room: 'Room 101', locationId: '1', type: 'courtroom' },
        { room: 'Room 102', locationId: '1', type: 'courtroom' },
      ],
    },
    {
      name: 'Surrey',
      shortName: 'SUR',
      code: 'SUR',
      locationId: '2',
      courtRooms: [
        { room: 'Room 201', locationId: '2', type: 'courtroom' },
        { room: 'Room 202', locationId: '2', type: 'courtroom' },
      ],
    },
  ];

  const mockDarsResults: DarsLogNote[] = [
    {
      date: '2025-10-28T09:00:00',
      locationId: 1,
      courtRoomCd: 'Room 101',
      url: 'https://example.com/recording1',
      fileName: 'recording1.mp3',
      locationNm: 'Vancouver',
    },
    {
      date: '2025-10-28T14:00:00',
      locationId: 1,
      courtRoomCd: 'Room 101',
      url: 'https://example.com/recording2',
      fileName: 'recording2.mp3',
      locationNm: 'Vancouver',
    },
  ];

  beforeEach(() => {
    setActivePinia(createPinia());
    snackbarStore = useSnackbarStore();
    darsStore = useDarsStore();

    mockDarsSearch.mockReset();
    mockGetLocations.mockReset();
    mockWindowOpen.mockReset();
    snackbarStore.showSnackbar = vi.fn();

    mockGetLocations.mockResolvedValue(mockLocations);
    mockDarsSearch.mockResolvedValue([]);

    darsStore.resetSearchCriteria();
  });

  it('renders correctly when opened', async () => {
    const wrapper = await mountComponent({ modelValue: true });
    await nextTick();

    expect(wrapper.find('v-dialog').exists()).toBe(true);
    expect(wrapper.text()).toContain('DARS Access');
  });

  describe('Prefill data population', () => {
    it('populates date, location, and room when prefill props are provided', async () => {
      const prefillDate = new Date('2025-10-28');
      const prefillLocationId = 1;
      const prefillRoom = 'Room 101';

      const wrapper = await mountComponent({
        modelValue: false,
        prefillDate,
        prefillLocationId,
        prefillRoom,
      });

      await nextTick();
      await flushPromises();
      await wrapper.setProps({ modelValue: true });

      expect(darsStore.searchDate).toEqual(prefillDate);
      expect(darsStore.searchLocation?.locationId).toBe('1');
      expect(darsStore.searchRoom).toBe(prefillRoom);
    });

    it('does not override existing search criteria when no prefill data provided', async () => {
      darsStore.searchDate = new Date('2025-10-25');
      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = 'Room 102';

      await mountComponent({
        modelValue: true,
      });
      await nextTick();
      await flushPromises();

      expect(darsStore.searchDate).toEqual(new Date('2025-10-25'));
      expect(darsStore.searchLocation).toEqual(mockLocations[0]);
      expect(darsStore.searchRoom).toBe('Room 102');
    });
  });

  describe('Location selection populates court rooms', () => {
    it('populates court rooms when a location is selected', async () => {
      await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      expect(darsStore.searchLocation.courtRooms).toHaveLength(2);
      expect(darsStore.searchLocation.courtRooms[0].room).toBe('Room 101');
      expect(darsStore.searchLocation.courtRooms[1].room).toBe('Room 102');
    });

    it('resets room selection when location changes', async () => {
      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = 'Room 101';
      await nextTick();

      // Change location - this should trigger handleLocationChange
      const vm = wrapper.vm as any;
      darsStore.searchLocation = mockLocations[1];
      vm.handleLocationChange();
      await nextTick();

      expect(darsStore.searchRoom).toBe('');
    });
  });

  describe('Search request with correct parameters', () => {
    it('fires search request with location, room, and date when search button clicked', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      const searchDate = new Date('2025-10-28');
      darsStore.searchDate = searchDate;
      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = 'Room 101';
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(mockDarsSearch).toHaveBeenCalledWith('2025-10-28', 1, 'Room 101');
    });

    it('fires search request without room when room is not selected', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      const searchDate = new Date('2025-10-28');
      darsStore.searchDate = searchDate;
      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = '';
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      // Verify search was called with empty room
      expect(mockDarsSearch).toHaveBeenCalledWith('2025-10-28', 1, '');
    });
  });

  describe('Required field validation', () => {
    it('requires date field to be filled', async () => {
      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = null;
      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = 'Room 101';
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(mockDarsSearch).not.toHaveBeenCalled();
    });

    it('requires location field to be filled', async () => {
      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = null;
      darsStore.searchRoom = 'Room 101';
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(mockDarsSearch).not.toHaveBeenCalled();
    });
  });

  describe('Results display as expected', () => {
    it('displays correct number of results', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = 'Room 101';
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      const resultItems = wrapper.findAll('.result-item');
      expect(resultItems).toHaveLength(2);
    });

    it('displays result content with correct formatting', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      const resultItems = wrapper.findAll('v-list-item');
      expect(resultItems[0].text()).toContain('Vancouver');
      expect(resultItems[0].text()).toContain('Room 101');
      expect(resultItems[0].text()).toContain('recording1.mp3');
      expect(resultItems[1].text()).toContain('recording2.mp3');
    });

    it('displays info alert when multiple results are returned', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(wrapper.find('v-alert').exists()).toBe(true);
      expect(wrapper.text()).toContain('Multiple audio recordings were found');
    });

    it('displays warning snackbar when no results found', async () => {
      mockDarsSearch.mockResolvedValue([]);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(snackbarStore.showSnackbar).toHaveBeenCalledWith(
        'No audio recordings found for the specified criteria.',
        'warning',
        'No Results'
      );
    });
  });

  describe('Clicking result opens link in new tab', () => {
    it('opens result URL in new tab when result row is clicked', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      const resultItems = wrapper.findAll('v-list-item');
      expect(resultItems[0].attributes('href')).toBe(
        'https://example.com/recording1'
      );
      expect(resultItems[0].attributes('target')).toBe('_blank');
      expect(resultItems[0].attributes('rel')).toBe('noopener noreferrer');
      expect(resultItems[1].attributes('href')).toBe(
        'https://example.com/recording2'
      );
    });

    it('auto-opens single result in new tab', async () => {
      const singleResult = [mockDarsResults[0]];
      mockDarsSearch.mockResolvedValue(singleResult);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(mockWindowOpen).toHaveBeenCalledWith(
        'https://example.com/recording1',
        '_blank',
        'noopener,noreferrer'
      );

      expect(snackbarStore.showSnackbar).toHaveBeenCalledWith(
        'Opening audio recording in new tab.',
        'success',
        'Success'
      );

      expect(wrapper.findAll('.result-item')).toHaveLength(1);
    });
  });

  describe('Loading state during search', () => {
    it('displays loading icon and disables search button while searching', async () => {
      let resolveSearch: (value: DarsLogNote[]) => void;
      const searchPromise = new Promise<DarsLogNote[]>((resolve) => {
        resolveSearch = resolve;
      });
      mockDarsSearch.mockReturnValue(searchPromise);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      const searchPromiseResult = vm.handleSearch();
      await nextTick();
      await flushPromises();

      expect(
        wrapper.find('[data-testid="loading-indicator"]').exists() ||
          wrapper.text().includes('Searching for audio recordings')
      ).toBe(true);

      const actionButtons = wrapper.findComponent({ name: 'action-buttons' });
      if (actionButtons.exists()) {
        expect(actionButtons.props('showSearch')).toBe(false);
      }

      resolveSearch!(mockDarsResults);
      await searchPromiseResult;
      await flushPromises();
      await nextTick();

      if (actionButtons.exists()) {
        expect(actionButtons.props('showSearch')).toBe(true);
      }
    });
  });

  describe('Previous search criteria persists when modal reopened', () => {
    it('retains search criteria after closing and reopening modal', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      const searchDate = new Date('2025-10-28');
      darsStore.searchDate = searchDate;
      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = 'Room 101';
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      await wrapper.setProps({ modelValue: false });
      await nextTick();

      await wrapper.setProps({ modelValue: true });
      await nextTick();
      await flushPromises();

      expect(darsStore.searchDate).toEqual(searchDate);
      expect(darsStore.searchLocation).toEqual(mockLocations[0]);
      expect(darsStore.searchRoom).toBe('Room 101');
    });
  });

  describe('Reset button functionality', () => {
    it('clears all search criteria when reset button is clicked', async () => {
      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      darsStore.searchRoom = 'Room 101';
      await nextTick();

      const vm = wrapper.vm as any;
      vm.resetForm();
      await nextTick();

      expect(darsStore.searchDate).toBeNull();
      expect(darsStore.searchLocation).toBeNull();
      expect(darsStore.searchRoom).toBe('');
    });

    it('clears search results when reset button is clicked', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(wrapper.findAll('.result-item')).toHaveLength(2);

      vm.resetForm();
      await nextTick();

      expect(wrapper.findAll('.result-item')).toHaveLength(0);
    });
  });

  describe('Error handling', () => {
    it('displays error snackbar when search fails', async () => {
      const errorMessage = 'Network error occurred';
      mockDarsSearch.mockRejectedValue(new Error(errorMessage));

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(snackbarStore.showSnackbar).toHaveBeenCalledWith(
        expect.stringContaining(errorMessage),
        'error',
        'Search Failed'
      );
    });

    it('displays warning snackbar when 404 error occurs', async () => {
      const error404 = {
        response: { status: 404 },
        message: 'Not found',
      };
      mockDarsSearch.mockRejectedValue(error404);

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const vm = wrapper.vm as any;
      await vm.handleSearch();
      await flushPromises();

      expect(snackbarStore.showSnackbar).toHaveBeenCalledWith(
        'No audio recordings found for the specified criteria.',
        'warning',
        'No Results'
      );
    });

    it('displays error snackbar when location loading fails', async () => {
      mockGetLocations.mockRejectedValue(new Error('Failed to load locations'));

      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      expect(snackbarStore.showSnackbar).toHaveBeenCalledWith(
        'Failed to load locations. Please try again.',
        'error',
        'Error'
      );
    });
  });

  describe('Modal close behavior', () => {
    it('closes modal when close button is clicked', async () => {
      const wrapper = await mountComponent({ modelValue: true });
      await nextTick();

      const vm = wrapper.vm as any;
      vm.close();
      await nextTick();

      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')![0]).toEqual([false]);
    });

    it('clears search results when modal is closed', async () => {
      mockDarsSearch.mockResolvedValue(mockDarsResults);

      await mountComponent({ modelValue: true });
      await nextTick();
      await flushPromises();

      darsStore.searchDate = new Date('2025-10-28');
      darsStore.searchLocation = mockLocations[0];
      await nextTick();

      const wrapper2 = await mountComponent({ modelValue: true });
      const vm = wrapper2.vm as any;
      await vm.handleSearch();
      await flushPromises();

      const resultItems = wrapper2.findAll('.result-item');
      expect(resultItems.length).toBeGreaterThan(0);

      await wrapper2.setProps({ modelValue: false });
      await nextTick();

      await wrapper2.setProps({ modelValue: true });
      await nextTick();

      const resultItemsAfter = wrapper2.findAll('.result-item');
      expect(resultItemsAfter).toHaveLength(0);
    });
  });
});
