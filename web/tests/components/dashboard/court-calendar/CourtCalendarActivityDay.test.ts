import CourtCalendarActivityDay from '@/components/dashboard/court-calendar/CourtCalendarActivityDay.vue';
import { CourtCalendarLocation } from '@/types';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';

const createActivity = (
  overrides: Partial<{
    activityCode: string;
    activityDisplayCode: string;
    activityDescription: string;
    activityClassCode: string;
    activityClassDescription: string;
    courtRooms: string[];
  }> = {}
) => ({
  activityCode: faker.string.alphanumeric(4),
  activityDisplayCode: faker.lorem.word(),
  activityDescription: faker.lorem.sentence(),
  activityClassCode: faker.string.alpha(3),
  activityClassDescription: faker.lorem.word(),
  courtRooms: [],
  ...overrides,
});

const createLocation = (
  overrides: Partial<CourtCalendarLocation> = {}
): CourtCalendarLocation => ({
  locationId: faker.number.int({ min: 1, max: 999 }).toString(),
  locationShortName: faker.location.city(),
  activities: [createActivity()],
  ...overrides,
});

const mountComponent = (
  props: Partial<{ locations: CourtCalendarLocation[]; date: Date }> = {}
) =>
  mount(CourtCalendarActivityDay, {
    props: {
      locations: [createLocation()],
      ...props,
    },
    global: {
      stubs: {
        'v-tooltip': {
          template: '<slot name="activator" :props="{}" />',
        },
      },
    },
  });

describe('CourtCalendarActivityDay.vue', () => {
  describe('location rendering', () => {
    it('renders a short name for each location', () => {
      const locations = [createLocation(), createLocation()];
      const wrapper = mountComponent({ locations });

      const shortNames = wrapper.findAll('[data-testid="short-name"]');
      expect(shortNames).toHaveLength(2);
      expect(shortNames[0].text()).toBe(locations[0].locationShortName);
      expect(shortNames[1].text()).toBe(locations[1].locationShortName);
    });

    it('renders an activities list for each location', () => {
      const locations = [createLocation(), createLocation()];
      const wrapper = mountComponent({ locations });

      expect(wrapper.findAll('[data-testid="activities"]')).toHaveLength(2);
    });

    it('renders nothing when locations is empty', () => {
      const wrapper = mountComponent({ locations: [] });

      expect(wrapper.findAll('[data-testid="short-name"]')).toHaveLength(0);
      expect(wrapper.findAll('[data-testid="activities"]')).toHaveLength(0);
    });
  });

  describe('activity rendering', () => {
    it('renders one li per activity', () => {
      const activities = [createActivity(), createActivity(), createActivity()];
      const location = createLocation({ activities });
      const wrapper = mountComponent({ locations: [location] });

      const items = wrapper.find('[data-testid="activities"]').findAll('li');
      expect(items).toHaveLength(3);
    });

    it('shows activityDisplayCode as the activity text', () => {
      const displayCode = 'TST';
      const activity = createActivity({ activityDisplayCode: displayCode });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      expect(wrapper.find('[data-testid="activities"]').text()).toContain(
        displayCode
      );
    });

    it('falls back to activityDescription when activityDisplayCode is absent', () => {
      const description = faker.lorem.sentence();
      const activity = createActivity({
        activityDisplayCode: undefined as unknown as string,
        activityDescription: description,
      });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      expect(wrapper.find('[data-testid="activities"]').text()).toContain(
        description
      );
    });

    it('renders court rooms in parentheses when present', () => {
      const activity = createActivity({ courtRooms: ['101', '202'] });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      expect(wrapper.find('[data-testid="activities"]').text()).toContain(
        '(101, 202)'
      );
    });

    it('does not render court rooms section when courtRooms is empty', () => {
      const activity = createActivity({ courtRooms: [] });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      expect(wrapper.find('[data-testid="activities"]').text()).not.toContain(
        '('
      );
    });
  });

  describe('activity CSS class from activityClassDescription', () => {
    it('applies a lowercased class derived from activityClassDescription', () => {
      const activity = createActivity({ activityClassDescription: 'Criminal' });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      const li = wrapper.find('[data-testid="activities"] li');
      expect(li.classes()).toContain('criminal');
    });

    it('replaces spaces with hyphens in the CSS class', () => {
      const activity = createActivity({
        activityClassDescription: 'Family Law',
      });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      const li = wrapper.find('[data-testid="activities"] li');
      expect(li.classes()).toContain('family-law');
    });

    it('trims leading and trailing spaces from activityClassDescription', () => {
      const activity = createActivity({
        activityClassDescription: '  Civil  ',
      });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      const li = wrapper.find('[data-testid="activities"] li');
      expect(li.classes()).toContain('civil');
    });

    it('collapses multiple consecutive spaces into a single hyphen', () => {
      const activity = createActivity({
        activityClassDescription: 'Small  Claims',
      });
      const wrapper = mountComponent({
        locations: [createLocation({ activities: [activity] })],
      });

      const li = wrapper.find('[data-testid="activities"] li');
      expect(li.classes()).toContain('small-claims');
    });
  });

  describe('multiple locations with multiple activities', () => {
    it('renders all activities across all locations', () => {
      const locations = [
        createLocation({ activities: [createActivity(), createActivity()] }),
        createLocation({ activities: [createActivity()] }),
      ];
      const wrapper = mountComponent({ locations });

      const allItems = wrapper.findAll('[data-testid="activities"] li');
      expect(allItems).toHaveLength(3);
    });
  });
});
