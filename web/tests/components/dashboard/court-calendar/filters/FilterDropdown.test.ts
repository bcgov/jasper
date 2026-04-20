import FilterDropdown from '@/components/dashboard/court-calendar/filters/FilterDropdown.vue';
import { TextValue } from '@/types/TextValue';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';
import { defineComponent, nextTick } from 'vue';

describe('FilterDropdown.vue', () => {
  let mockItems: TextValue[];

  const createItem = (overrides?: Partial<TextValue>): TextValue => ({
    value: faker.string.uuid(),
    text: faker.location.city(),
    ...overrides,
  });

  beforeEach(() => {
    mockItems = [
      createItem({ value: 'A', text: 'Alpha' }),
      createItem({ value: 'B', text: 'Beta' }),
      createItem({ value: 'C', text: 'Gamma' }),
    ];
  });

  /**
   * Stubs FilterDropdownBase with a simple div that always renders the slot,
   * allowing list items to be found directly in the wrapper DOM.
   * A "open-btn" button is provided to trigger the onMenuOpen handler.
   */
  const createBaseStub = (searchQuery = '') =>
    defineComponent({
      name: 'FilterDropdownBase',
      props: ['title', 'selectedCount', 'showSearch'],
      emits: ['open'],
      setup() {
        return { sq: searchQuery };
      },
      template: `<div><button data-testid="open-btn" @click="$emit('open')" /><slot :searchQuery="sq" /></div>`,
    });

  const mountComponent = (props = {}, searchQuery = '') =>
    mount(FilterDropdown, {
      props: {
        title: 'Test Filter',
        items: mockItems,
        modelValue: [],
        ...props,
      },
      global: {
        stubs: {
          FilterDropdownBase: createBaseStub(searchQuery),
        },
      },
    });

  describe('FilterDropdownBase props', () => {
    it('passes title prop to FilterDropdownBase', () => {
      const title = faker.word.noun();
      const wrapper = mountComponent({ title });

      expect(wrapper.findComponent({ name: 'FilterDropdownBase' }).props('title')).toBe(title);
    });

    it('passes selectedCount equal to the number of selected items', () => {
      const wrapper = mountComponent({ modelValue: ['A', 'B'] });

      expect(wrapper.findComponent({ name: 'FilterDropdownBase' }).props('selectedCount')).toBe(2);
    });

    it('passes showSearch prop to FilterDropdownBase', () => {
      const wrapper = mountComponent({ showSearch: false });

      expect(wrapper.findComponent({ name: 'FilterDropdownBase' }).props('showSearch')).toBe(false);
    });

    it('defaults showSearch to true', () => {
      const wrapper = mountComponent();

      expect(wrapper.findComponent({ name: 'FilterDropdownBase' }).props('showSearch')).toBe(true);
    });
  });

  describe('Select All', () => {
    it('renders Select All list item when showSelectAll is true', () => {
      const wrapper = mountComponent({ showSelectAll: true });

      const hasSelectAll = wrapper
        .findAll('v-list-item')
        .some((li) => li.attributes('title') === 'Select All');

      expect(hasSelectAll).toBe(true);
    });

    it('does not render Select All list item when showSelectAll is false', () => {
      const wrapper = mountComponent({ showSelectAll: false });

      const hasSelectAll = wrapper
        .findAll('v-list-item')
        .some((li) => li.attributes('title') === 'Select All');

      expect(hasSelectAll).toBe(false);
    });

    it('renders divider when showSelectAll is true', () => {
      const wrapper = mountComponent({ showSelectAll: true });

      expect(wrapper.find('v-divider').exists()).toBe(true);
    });

    it('does not render divider when showSelectAll is false', () => {
      const wrapper = mountComponent({ showSelectAll: false });

      expect(wrapper.find('v-divider').exists()).toBe(false);
    });

    it('emits update:modelValue with all item values when not all items are selected', async () => {
      const wrapper = mountComponent({ modelValue: [] });

      const selectAllItem = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Select All');
      await selectAllItem!.trigger('click');

      const emitted = wrapper.emitted('update:modelValue')![0][0] as string[];
      expect(emitted).toHaveLength(3);
      expect(emitted).toEqual(expect.arrayContaining(['A', 'B', 'C']));
    });

    it('emits empty array when Select All is clicked and all items are already selected', async () => {
      const wrapper = mountComponent({ modelValue: ['A', 'B', 'C'] });

      const selectAllItem = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Select All');
      await selectAllItem!.trigger('click');

      expect(wrapper.emitted('update:modelValue')![0][0]).toEqual([]);
    });
  });

  describe('Selected items section', () => {
    it('shows selected items at the top and excludes them from the main list after menu opens', async () => {
      const wrapper = mountComponent({ modelValue: ['A'], showSelected: true });
      await wrapper.find('[data-testid="open-btn"]').trigger('click');
      await nextTick();

      const titles = wrapper.findAll('v-list-item').map((li) => li.attributes('title'));

      // Alpha exists in the selected section
      expect(titles).toContain('Alpha');
      // Alpha is not duplicated in the main list (appears exactly once)
      expect(titles.filter((t) => t === 'Alpha')).toHaveLength(1);
      // Unselected items still appear in main list
      expect(titles).toContain('Beta');
      expect(titles).toContain('Gamma');
    });

    it('shows all items in the main list and hides the selected section when showSelected is false', async () => {
      const wrapper = mountComponent({ modelValue: ['A'], showSelected: false });
      await wrapper.find('[data-testid="open-btn"]').trigger('click');
      await nextTick();

      const titles = wrapper.findAll('v-list-item').map((li) => li.attributes('title'));

      // All items present in main list (selected section is not rendered)
      expect(titles).toContain('Alpha');
      expect(titles).toContain('Beta');
      expect(titles).toContain('Gamma');
      // Each item appears exactly once (no selected section duplication)
      expect(titles.filter((t) => t === 'Alpha')).toHaveLength(1);
    });
  });

  describe('filteredItems', () => {
    it('filters main list items by search query', () => {
      // 'al' matches 'Alpha' only
      const wrapper = mountComponent({ modelValue: [] }, 'al');

      const titles = wrapper
        .findAll('v-list-item')
        .map((li) => li.attributes('title'))
        .filter((t) => t !== 'Select All');

      expect(titles).toContain('Alpha');
      expect(titles).not.toContain('Beta');
      expect(titles).not.toContain('Gamma');
    });

    it('returns all items when showSelected is false, ignoring the search query', () => {
      const wrapper = mountComponent({ showSelected: false }, 'al');

      const titles = wrapper
        .findAll('v-list-item')
        .map((li) => li.attributes('title'))
        .filter((t) => t !== 'Select All');

      expect(titles).toContain('Alpha');
      expect(titles).toContain('Beta');
      expect(titles).toContain('Gamma');
    });
  });

  describe('toggleItem', () => {
    it('emits update:modelValue with item added when an unselected item is clicked', async () => {
      const wrapper = mountComponent({ modelValue: [] });

      const alphaItem = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Alpha');
      await alphaItem!.trigger('click');

      expect(wrapper.emitted('update:modelValue')![0][0]).toEqual(['A']);
    });

    it('emits update:modelValue with item removed when a selected item is clicked', async () => {
      const wrapper = mountComponent({ modelValue: ['A'] });
      await wrapper.find('[data-testid="open-btn"]').trigger('click');
      await nextTick();

      // After menu opens, Alpha appears in the selected section
      const alphaItem = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Alpha');
      await alphaItem!.trigger('click');

      expect(wrapper.emitted('update:modelValue')![0][0]).toEqual([]);
    });

    it('preserves other selections when toggling a single item', async () => {
      const wrapper = mountComponent({ modelValue: ['A', 'B'] });

      const gammaItem = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Gamma');
      await gammaItem!.trigger('click');

      const emitted = wrapper.emitted('update:modelValue')![0][0] as string[];
      expect(emitted).toEqual(expect.arrayContaining(['A', 'B', 'C']));
    });
  });
});
