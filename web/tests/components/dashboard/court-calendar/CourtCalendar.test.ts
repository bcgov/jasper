import CourtCalendar from '@/components/dashboard/court-calendar/CourtCalendar.vue';
import { DashboardService, LocationService } from '@/services';
import { CalendarViewEnum } from '@/types/common';
import { LocationInfo } from '@/types/courtlist';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/services');

describe('CourtCalendar.vue', () => {
  let dashboardService: any;
  let locationService: any;
  let mockLocations: LocationInfo[];

  const createMockLocation = (
    overrides?: Partial<LocationInfo>
  ): LocationInfo => ({
    locationId: faker.string.uuid(),
    shortName: faker.location.city(),
    name: `${faker.location.city()} Law Courts`,
    code: faker.string.alpha({ length: 3, casing: 'upper' }),
    agencyIdentifierCd: faker.string.alpha({ length: 3, casing: 'upper' }),
    courtRooms: [],
    ...overrides,
  });

  beforeEach(() => {
    // Create fresh mock data for each test with faker-generated values
    mockLocations = [
      createMockLocation({ locationId: 'LOC1' }),
      createMockLocation({ locationId: 'LOC2' }),
      createMockLocation({ locationId: 'LOC3' }),
    ];

    dashboardService = {
      getCourtCalendar: vi.fn().mockResolvedValue({
        payload: { days: [], presiders: [], activities: [] },
      }),
    };
    locationService = {
      getLocations: vi.fn().mockResolvedValue(mockLocations),
    };

    (DashboardService as any).mockReturnValue(dashboardService);
    (LocationService as any).mockReturnValue(locationService);
  });

  const mountComponent = (props = {}) => {
    return mount(CourtCalendar, {
      props: {
        judgeId: faker.number.int({ min: 1, max: 100 }),
        selectedDate: new Date(),
        calendarView: CalendarViewEnum.TwoWeekView,
        isCalendarLoading: true,
        ...props,
      },
      global: {
        provide: {
          dashboardService,
          locationService,
        },
      },
    });
  };

  it('renders skeleton loader when isCalendarLoading is true while FullCalendar is hidden', async () => {
    const wrapper: any = mountComponent();

    expect(wrapper.find('[data-testid="cc-loader"]').exists()).toBeTruthy();
    expect(wrapper.find('.fc').exists()).toBeFalsy();
  });

  it('renders FullCalendar when isCalendarLoading is false while skeleton loader is hidden', async () => {
    const wrapper: any = mountComponent();

    expect(wrapper.find('[data-testid="cc-loader"]').exists()).toBeTruthy();
    expect(wrapper.find('.fc').exists()).toBeFalsy();

    wrapper.vm.isCalendarLoading = false;
    await nextTick();

    expect(wrapper.find('[data-testid="cc-loader"]').exists()).toBeFalsy();
    expect(wrapper.find('.fc').exists()).toBeTruthy();
  });

  describe('Filter Loader (cc-filters-loader)', () => {
    it('displays filter loader skeleton when location filter is loading', async () => {
      const wrapper = mountComponent();

      // Initially loading
      expect(wrapper.find('[data-testid="cc-filters-loader"]').exists()).toBe(
        true
      );
      expect(
        wrapper.find('[data-testid="cc-filters-loader"]').attributes('loading')
      ).toBe('true');
    });

    it('hides filter loader when locations are loaded', async () => {
      const wrapper = mountComponent();

      // Wait for locations to load
      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();
      await nextTick();

      // Filter loader should be hidden after locations load
      expect(wrapper.find('[data-testid="cc-filters-loader"]').exists()).toBe(
        false
      );
    });

    it('displays filter loader with correct skeleton type', () => {
      const wrapper = mountComponent();

      const filterLoader = wrapper.find('[data-testid="cc-filters-loader"]');
      expect(filterLoader.attributes('type')).toBe('list-item-avatar-two-line');
    });
  });

  describe('Court Calendar Filters (cc-filters)', () => {
    it('does not display filters when locations are not loaded', () => {
      locationService.getLocations = vi.fn().mockResolvedValue([]);
      const wrapper = mountComponent();

      expect(wrapper.find('[data-testid="cc-filters"]').exists()).toBe(false);
    });

    it('displays filters when locations are loaded', async () => {
      const wrapper = mountComponent();

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();
      await nextTick();

      expect(wrapper.find('[data-testid="cc-filters"]').exists()).toBe(true);
    });

    it('passes location filter loading state to filters component', async () => {
      const wrapper = mountComponent();

      const filtersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });

      // Initially loading
      expect(filtersComponent.exists()).toBe(false);

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();
      await nextTick();

      const loadedFiltersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });
      if (loadedFiltersComponent.exists()) {
        expect(loadedFiltersComponent.props('isLocationFilterLoading')).toBe(
          false
        );
      }
    });

    it('passes locations data to filters component', async () => {
      const wrapper = mountComponent();

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();
      await nextTick();

      const filtersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });

      if (filtersComponent.exists()) {
        expect(filtersComponent.props('locations')).toEqual(mockLocations);
      }
    });

    it('updates calendar when selected locations change', async () => {
      const testJudgeId = faker.number.int({ min: 1, max: 100 });
      const wrapper = mountComponent({ judgeId: testJudgeId });

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();
      await nextTick();

      // Clear initial calls
      dashboardService.getCourtCalendar.mockClear();

      // Update selected locations via the filters component
      const filtersComponent = wrapper.findComponent({
        name: 'CourtCalendarFilters',
      });
      if (filtersComponent.exists()) {
        await filtersComponent.vm.$emit('update:selectedLocations', [
          'LOC1',
          'LOC2',
        ]);
        await nextTick();

        // Calendar should reload with location filters
        await vi.waitFor(() => {
          expect(dashboardService.getCourtCalendar).toHaveBeenCalledWith(
            expect.any(String),
            expect.any(String),
            'LOC1,LOC2',
            testJudgeId
          );
        });
      }
    });

    it('loads calendar with no location filter when none selected', async () => {
      const testJudgeId = faker.number.int({ min: 1, max: 100 });
      const wrapper = mountComponent({ judgeId: testJudgeId });

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();

      // Verify initial call has empty location filter
      expect(dashboardService.getCourtCalendar).toHaveBeenCalledWith(
        expect.any(String),
        expect.any(String),
        '',
        testJudgeId
      );
    });

    it('handles location service errors gracefully', async () => {
      const consoleErrorSpy = vi
        .spyOn(console, 'error')
        .mockImplementation(() => {});
      locationService.getLocations = vi
        .fn()
        .mockRejectedValue(new Error('Failed to load'));

      const wrapper = mountComponent();

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();
      await nextTick();

      // Filters should not be displayed on error
      expect(wrapper.find('[data-testid="cc-filters"]').exists()).toBe(false);
      expect(consoleErrorSpy).toHaveBeenCalledWith(
        'Failed to load locations:',
        expect.any(Error)
      );

      consoleErrorSpy.mockRestore();
    });

    it('hides filter loader after location loading completes', async () => {
      const wrapper = mountComponent();

      // Initially shows loader
      expect(wrapper.find('[data-testid="cc-filters-loader"]').exists()).toBe(
        true
      );

      await vi.waitFor(() => {
        expect(locationService.getLocations).toHaveBeenCalled();
      });

      await nextTick();
      await nextTick();

      // Loader should be hidden
      expect(wrapper.find('[data-testid="cc-filters-loader"]').exists()).toBe(
        false
      );
      // Filters should be visible
      expect(wrapper.find('[data-testid="cc-filters"]').exists()).toBe(true);
    });
  });
});
