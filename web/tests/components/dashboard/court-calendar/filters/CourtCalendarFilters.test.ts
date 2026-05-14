import ActivityClassFilter from '@/components/dashboard/court-calendar/filters/ActivityClassFilter.vue';
import CourtCalendarFilters from '@/components/dashboard/court-calendar/filters/CourtCalendarFilters.vue';
import FilterDropdown from '@/components/dashboard/court-calendar/filters/FilterDropdown.vue';
import FilterDropdownGrouped from '@/components/dashboard/court-calendar/filters/FilterDropdownGrouped.vue';
import { useCommonStore } from '@/stores';
import { Activity, Presider } from '@/types';
import { RolesEnum, UserInfo } from '@/types/common';
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

  const createActivity = (overrides?: Partial<Activity>): Activity => ({
    code: faker.string.alpha({ length: 3, casing: 'upper' }),
    description: faker.lorem.words(3),
    classCode: faker.string.alpha({ length: 2, casing: 'upper' }),
    classDescription: faker.lorem.word(),
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
        activities: [],
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

  describe('Activities FilterDropdown', () => {
    it('does not render the Activities FilterDropdown when isPresidersView is true', () => {
      const activity = createActivity();
      const wrapper = mountComponent({
        activities: [activity],
        isPresidersView: true,
      });
      const dropdowns = wrapper.findAllComponents(FilterDropdown);
      expect(dropdowns.every((d) => d.props('title') !== 'Activities')).toBe(
        true
      );
    });

    it('does not render the Activities FilterDropdown when activities is empty, even if isPresidersView is false', () => {
      const wrapper = mountComponent({
        activities: [],
        isPresidersView: false,
      });
      const dropdowns = wrapper.findAllComponents(FilterDropdown);
      expect(dropdowns.every((d) => d.props('title') !== 'Activities')).toBe(
        true
      );
    });

    it('renders the Activities FilterDropdown when activities are present and isPresidersView is false', () => {
      const activity = createActivity();
      const wrapper = mountComponent({
        activities: [activity],
        isPresidersView: false,
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities');
      expect(activityDropdown).toBeDefined();
    });

    it('passes title "Activities" to the Activities FilterDropdown', () => {
      const activity = createActivity();
      const wrapper = mountComponent({
        activities: [activity],
        isPresidersView: false,
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities');
      expect(activityDropdown!.props('title')).toBe('Activities');
    });

    it('maps activities to { value: code, text: description, color } items', () => {
      const act1 = createActivity({
        code: 'TRL',
        description: 'Trial',
        classDescription: 'Criminal',
      });
      const act2 = createActivity({
        code: 'HRG',
        description: 'Hearing',
        classDescription: 'Civil',
      });
      const wrapper = mountComponent({
        activities: [act1, act2],
        isPresidersView: false,
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities')!;
      expect(activityDropdown.props('items')).toEqual([
        { value: 'TRL', text: 'Trial', color: 'criminal' },
        { value: 'HRG', text: 'Hearing', color: 'civil' },
      ]);
    });

    it('trims leading and trailing whitespace from classDescription for color', () => {
      const act = createActivity({
        code: 'X',
        description: 'X',
        classDescription: '  Small Claims  ',
      });
      const wrapper = mountComponent({
        activities: [act],
        isPresidersView: false,
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities')!;
      expect(activityDropdown.props('items')[0].color).toBe('small-claims');
    });

    it('passes showSelectAll=false to the Activities FilterDropdown', () => {
      const wrapper = mountComponent({
        activities: [createActivity()],
        isPresidersView: false,
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities')!;
      expect(activityDropdown.props('showSelectAll')).toBe(false);
    });

    it('passes showSearch=false to the Activities FilterDropdown', () => {
      const wrapper = mountComponent({
        activities: [createActivity()],
        isPresidersView: false,
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities')!;
      expect(activityDropdown.props('showSearch')).toBe(false);
    });

    it('passes selectedActivities as modelValue to the Activities FilterDropdown', () => {
      const act = createActivity({ code: 'TRL' });
      const wrapper = mountComponent({
        activities: [act],
        isPresidersView: false,
        selectedActivities: ['TRL'],
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities')!;
      expect(activityDropdown.props('modelValue')).toEqual(['TRL']);
    });

    it('emits update:selectedActivities when Activities FilterDropdown emits update:modelValue', async () => {
      const act = createActivity({ code: 'TRL' });
      const wrapper = mountComponent({
        activities: [act],
        isPresidersView: false,
        selectedActivities: [],
      });
      const activityDropdown = wrapper
        .findAllComponents(FilterDropdown)
        .find((d) => d.props('title') === 'Activities')!;
      await activityDropdown.vm.$emit('update:modelValue', ['TRL']);
      expect(wrapper.emitted('update:selectedActivities')?.[0]).toEqual([
        ['TRL'],
      ]);
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

    it('renders the Clear All button when selectedActivities is not empty', () => {
      const wrapper = mountComponent({
        selectedLocations: [],
        selectedActivities: ['TRL'],
        selectedActivityClass: 'all',
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

    it('emits empty array for selectedActivities when Clear All is clicked', async () => {
      const act = createActivity({ code: 'TRL' });
      const wrapper = mountComponent({
        activities: [act],
        isPresidersView: false,
        selectedActivities: ['TRL'],
        selectedLocations: [],
      });

      await wrapper.find('.clearAll').trigger('click');

      expect(wrapper.emitted('update:selectedActivities')?.at(-1)).toEqual([
        [],
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

  describe('canToggleView', () => {
    const setUserRoles = (roles: RolesEnum[]) => {
      const commonStore = useCommonStore();
      commonStore.userInfo = {
        roles,
        userType: 'Staff',
        enableArchive: false,
        subRole: '',
        isSupremeUser: 'false',
        isActive: true,
        agencyCode: 'TST',
        userId: faker.string.uuid(),
        judgeId: faker.number.int({ min: 1, max: 9999 }),
        judgeHomeLocationId: faker.number.int({ min: 1, max: 100 }),
        email: faker.internet.email(),
        userTitle: '',
      } satisfies UserInfo;
    };

    it('hides the view toggle when the user has no allowed role', () => {
      setUserRoles([RolesEnum.Judge]);
      const wrapper = mountComponent();
      expect(wrapper.find('[data-testid="toggleView"]').exists()).toBe(false);
    });

    it.each([
      [RolesEnum.Raj],
      [RolesEnum.AcjChiefJudge],
      [RolesEnum.PoManager],
      [RolesEnum.Admin],
    ])('shows the view toggle for role %s', (role) => {
      setUserRoles([role]);
      const wrapper = mountComponent();
      expect(wrapper.find('[data-testid="toggleView"]').exists()).toBe(true);
    });

    it('shows the Presiders and Activities buttons inside the toggle', () => {
      setUserRoles([RolesEnum.Admin]);
      const wrapper = mountComponent();
      const toggle = wrapper.find('[data-testid="toggleView"]');

      expect(toggle.text()).toContain('Presiders');
      expect(toggle.text()).toContain('Activities');
    });
  });
});
