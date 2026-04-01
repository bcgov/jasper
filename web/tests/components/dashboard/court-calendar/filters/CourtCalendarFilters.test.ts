import ActivityClassFilter from '@/components/dashboard/court-calendar/filters/ActivityClassFilter.vue';
import CourtCalendarFilters from '@/components/dashboard/court-calendar/filters/CourtCalendarFilters.vue';
import FilterDropdown from '@/components/dashboard/court-calendar/filters/FilterDropdown.vue';
import FilterDropdownGrouped from '@/components/dashboard/court-calendar/filters/FilterDropdownGrouped.vue';
import { Presider } from '@/types';
import { LocationInfo } from '@/types/courtlist';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

describe('CourtCalendarFilters.vue', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  const createLocation = (overrides?: Partial<LocationInfo>): LocationInfo => ({
    locationId: faker.string.uuid(),
    shortName: faker.location.city(),
    name: faker.location.city() + ' Law Courts',
    code: faker.string.alpha({ length: 3, casing: 'upper' }),
    agencyIdentifierCd: faker.string.alpha({ length: 3, casing: 'upper' }),
    courtRooms: [],
    ...overrides,
  });

  const createPresider = (overrides?: Partial<Presider>): Presider => ({
    id: faker.number.int({ min: 1, max: 9999 }),
    name: faker.person.fullName(),
    initials: faker.string.alpha({ length: 2, casing: 'upper' }),
    homeLocationId: faker.number.int({ min: 1, max: 9999 }),
    homeLocationName: faker.location.city(),
    ...overrides,
  });

  const mountComponent = (props = {}) =>
    mount(CourtCalendarFilters, {
      props: {
        isLocationFilterLoading: false,
        locations: [],
        presiders: [],
        judgeHomeLocationId: '',
        selectedLocations: [],
        selectedPresiders: [],
        selectedActivityClass: 'all',
        ...props,
      },
    });

  describe('ActivityClassFilter', () => {
    it('renders ActivityClassFilter', () => {
      const wrapper = mountComponent();
      expect(wrapper.findComponent(ActivityClassFilter).exists()).toBe(true);
    });

    it('passes selectedActivityClass to ActivityClassFilter as modelValue', () => {
      const wrapper = mountComponent({ selectedActivityClass: 'SIT' });
      expect(
        wrapper.findComponent(ActivityClassFilter).props()['modelValue']
      ).toBe('SIT');
    });

    it('emits update:selectedActivityClass when ActivityClassFilter emits update:modelValue', async () => {
      const wrapper = mountComponent({ selectedActivityClass: 'all' });
      await wrapper
        .findComponent(ActivityClassFilter)
        .vm.$emit('update:modelValue', 'NS');
      expect(wrapper.emitted('update:selectedActivityClass')?.[0]).toEqual([
        'NS',
      ]);
    });
  });

  describe('Locations FilterDropdown', () => {
    it('renders FilterDropdown', () => {
      const wrapper = mountComponent();
      expect(wrapper.findComponent(FilterDropdown).exists()).toBe(true);
    });

    it('passes "Locations" as title to FilterDropdown', () => {
      const wrapper = mountComponent();
      expect(wrapper.findComponent(FilterDropdown).props('title')).toBe(
        'Locations'
      );
    });

    it('maps locations to { value: locationId, text: shortName } items', () => {
      const loc1 = createLocation({ locationId: 'LOC1', shortName: 'VIC' });
      const loc2 = createLocation({ locationId: 'LOC2', shortName: 'VAN' });
      const wrapper = mountComponent({ locations: [loc1, loc2] });

      expect(wrapper.findComponent(FilterDropdown).props('items')).toEqual([
        { value: 'LOC1', text: 'VIC' },
        { value: 'LOC2', text: 'VAN' },
      ]);
    });
  });

  describe('Presiders FilterDropdownGrouped visibility', () => {
    it('does not render FilterDropdownGrouped when no locations are selected', () => {
      const wrapper = mountComponent({ selectedLocations: [] });
      expect(wrapper.findComponent(FilterDropdownGrouped).exists()).toBe(false);
    });

    it('renders FilterDropdownGrouped when at least one location is selected', () => {
      const loc = createLocation({ locationId: 'LOC1' });
      const wrapper = mountComponent({
        locations: [loc],
        selectedLocations: ['LOC1'],
      });
      expect(wrapper.findComponent(FilterDropdownGrouped).exists()).toBe(true);
    });

    it('passes "Presiders" as title to FilterDropdownGrouped', () => {
      const loc = createLocation({ locationId: 'LOC1' });
      const wrapper = mountComponent({
        locations: [loc],
        selectedLocations: ['LOC1'],
      });
      expect(wrapper.findComponent(FilterDropdownGrouped).props('title')).toBe(
        'Presiders'
      );
    });
  });

  describe('presiderItems grouping', () => {
    it('groups presiders by their home location shortName', () => {
      const loc1 = createLocation({ locationId: '1', shortName: 'VIC' });
      const loc2 = createLocation({ locationId: '2', shortName: 'VAN' });
      const presider1 = createPresider({
        id: 10,
        homeLocationId: 1,
        initials: 'AS',
        name: 'Alice Smith',
      });
      const presider2 = createPresider({
        id: 20,
        homeLocationId: 2,
        initials: 'BJ',
        name: 'Bob Jones',
      });

      const wrapper = mountComponent({
        locations: [loc1, loc2],
        presiders: [presider1, presider2],
        selectedLocations: ['1'],
      });

      const groups: any[] = wrapper
        .findComponent(FilterDropdownGrouped)
        .props('groups');
      expect(groups).toContainEqual(
        expect.objectContaining({
          label: 'VIC',
          items: [{ value: '10', text: 'AS - Alice Smith' }],
        })
      );
      expect(groups).toContainEqual(
        expect.objectContaining({
          label: 'VAN',
          items: [{ value: '20', text: 'BJ - Bob Jones' }],
        })
      );
    });

    it('sorts groups alphabetically by label', () => {
      const loc1 = createLocation({ locationId: '1', shortName: 'VIC' });
      const loc2 = createLocation({ locationId: '2', shortName: 'ABB' });
      const loc3 = createLocation({ locationId: '3', shortName: 'NEW' });
      const wrapper = mountComponent({
        locations: [loc1, loc2, loc3],
        presiders: [
          createPresider({ homeLocationId: 1 }),
          createPresider({ homeLocationId: 2 }),
          createPresider({ homeLocationId: 3 }),
        ],
        selectedLocations: ['1'],
      });

      const labels: string[] = wrapper
        .findComponent(FilterDropdownGrouped)
        .props('groups')
        .map((g: any) => g.label);
      expect(labels).toEqual([...labels].sort());
    });

    it('sorts items within each group alphabetically by text', () => {
      const loc = createLocation({ locationId: '1', shortName: 'VIC' });
      const presiderZ = createPresider({
        id: 1,
        homeLocationId: 1,
        initials: 'ZZ',
        name: 'Zara',
      });
      const presiderA = createPresider({
        id: 2,
        homeLocationId: 1,
        initials: 'AA',
        name: 'Aaron',
      });

      const wrapper = mountComponent({
        locations: [loc],
        presiders: [presiderZ, presiderA],
        selectedLocations: ['1'],
      });

      const groups: any[] = wrapper
        .findComponent(FilterDropdownGrouped)
        .props('groups');
      const vicGroup = groups.find((g) => g.label === 'VIC');
      expect(vicGroup.items[0].text).toContain('AA - Aaron');
      expect(vicGroup.items[1].text).toContain('ZZ - Zara');
    });

    it('places presiders with no matching location into an "Unknown" group', () => {
      const loc = createLocation({ locationId: '1', shortName: 'VIC' });
      const unmatched = createPresider({
        id: 99,
        homeLocationId: 9999,
        initials: 'XX',
        name: 'Unmatched',
      });

      const wrapper = mountComponent({
        locations: [loc],
        presiders: [unmatched],
        selectedLocations: ['1'],
      });

      const groups: any[] = wrapper
        .findComponent(FilterDropdownGrouped)
        .props('groups');
      const unknownGroup = groups.find((g) => g.label === 'Unknown');
      expect(unknownGroup).toBeDefined();
      expect(unknownGroup.items).toContainEqual({
        value: '99',
        text: 'XX - Unmatched',
      });
    });
  });

  describe('Clear All button', () => {
    it('does not render the Clear All button when no locations are selected and activity class is "all"', () => {
      const wrapper = mountComponent({
        selectedLocations: [],
        selectedActivityClass: 'all',
      });
      expect(wrapper.find('.clearAll').exists()).toBe(false);
    });

    it('renders the Clear All button when locations are selected', () => {
      const loc = createLocation({ locationId: 'LOC1' });
      const wrapper = mountComponent({
        locations: [loc],
        selectedLocations: ['LOC1'],
      });
      expect(wrapper.find('.clearAll').exists()).toBe(true);
    });

    it('renders the Clear All button when selectedActivityClass is not "all"', () => {
      const wrapper = mountComponent({
        selectedLocations: [],
        selectedActivityClass: 'SIT',
      });
      expect(wrapper.find('.clearAll').exists()).toBe(true);
    });

    it('emits empty arrays for both location/presider models and resets activity class when Clear All is clicked', async () => {
      const loc = createLocation({ locationId: 'LOC1' });
      const presider = createPresider({ id: 1, homeLocationId: 1 });
      const wrapper = mountComponent({
        locations: [loc],
        presiders: [presider],
        selectedLocations: ['LOC1'],
        selectedPresiders: ['1'],
        selectedActivityClass: 'SIT',
      });

      await wrapper.find('.clearAll').trigger('click');

      expect(wrapper.emitted('update:selectedLocations')?.at(-1)).toEqual([
        [wrapper.props('judgeHomeLocationId')],
      ]);
      expect(wrapper.emitted('update:selectedPresiders')?.at(-1)).toEqual([[]]);
      expect(wrapper.emitted('update:selectedActivityClass')?.at(-1)).toEqual([
        'all',
      ]);
    });
  });

  describe('Presider selection is not auto-updated when presiderItems changes', () => {
    it('does not auto-emit selectedPresiders when presiders prop is updated', async () => {
      const loc = createLocation({ locationId: '1', shortName: 'VIC' });
      const wrapper = mountComponent({
        locations: [loc],
        selectedLocations: ['1'],
        presiders: [],
        selectedPresiders: [],
      });

      await wrapper.setProps({
        presiders: [
          createPresider({ id: 10, homeLocationId: 1 }),
          createPresider({ id: 20, homeLocationId: 1 }),
        ],
      });
      await nextTick();

      const emitted = wrapper.emitted('update:selectedPresiders');
      expect(emitted).toBeUndefined();
    });
  });
});
