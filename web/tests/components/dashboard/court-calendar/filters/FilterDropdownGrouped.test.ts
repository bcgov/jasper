import FilterDropdownGrouped from '@/components/dashboard/court-calendar/filters/FilterDropdownGrouped.vue';
import { ItemGroup } from '@/types';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';
import { defineComponent } from 'vue';

describe('FilterDropdownGrouped.vue', () => {
  let mockGroups: ItemGroup[];

  const createGroup = (
    label: string,
    items: { value: string; text: string }[]
  ): ItemGroup => ({ label, items });

  beforeEach(() => {
    mockGroups = [
      createGroup('Victoria', [
        { value: 'A1', text: 'Alice' },
        { value: 'A2', text: 'Andrew' },
      ]),
      createGroup('Vancouver', [{ value: 'B1', text: 'Bob' }]),
    ];
  });

  // Stubs FilterDropdownBase so the slot is always rendered, with a
  // configurable searchQuery passed through the scoped slot.
  const createBaseStub = (searchQuery = '') =>
    defineComponent({
      name: 'FilterDropdownBase',
      props: ['title', 'selectedCount', 'showSearch'],
      setup() {
        return { sq: searchQuery };
      },
      template: `<div><slot :menu="false" :searchQuery="sq" /></div>`,
    });

  const mountComponent = (props = {}, searchQuery = '') =>
    mount(FilterDropdownGrouped, {
      props: {
        title: faker.word.noun(),
        groups: mockGroups,
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
    it('passes title to FilterDropdownBase', () => {
      const title = faker.word.noun();
      const wrapper = mountComponent({ title });

      expect(
        wrapper.findComponent({ name: 'FilterDropdownBase' }).props('title')
      ).toBe(title);
    });

    it('passes selectedCount equal to modelValue length', () => {
      const wrapper = mountComponent({ modelValue: ['A1', 'B1'] });

      expect(
        wrapper
          .findComponent({ name: 'FilterDropdownBase' })
          .props('selectedCount')
      ).toBe(2);
    });

    it('passes showSearch prop to FilterDropdownBase', () => {
      const wrapper = mountComponent({ showSearch: false });

      expect(
        wrapper
          .findComponent({ name: 'FilterDropdownBase' })
          .props('showSearch')
      ).toBe(false);
    });

    it('defaults showSearch to true', () => {
      const wrapper = mountComponent();

      expect(
        wrapper
          .findComponent({ name: 'FilterDropdownBase' })
          .props('showSearch')
      ).toBe(true);
    });
  });

  describe('Group rendering', () => {
    it('renders a heading for each group', () => {
      const wrapper = mountComponent();

      const headings = wrapper.findAll('h6').map((h) => h.text());
      expect(headings).toContain('Victoria');
      expect(headings).toContain('Vancouver');
    });

    it('renders a list item for each item in every group', () => {
      const wrapper = mountComponent();

      const titles = wrapper
        .findAll('v-list-item')
        .map((li) => li.attributes('title'));

      expect(titles).toContain('Alice');
      expect(titles).toContain('Andrew');
      expect(titles).toContain('Bob');
    });
  });

  describe('filteredGroups — search query', () => {
    it('shows all groups when searchQuery is empty', () => {
      const wrapper = mountComponent({}, '');

      const headings = wrapper.findAll('h6').map((h) => h.text());
      expect(headings).toContain('Victoria');
      expect(headings).toContain('Vancouver');
    });

    it('filters items within groups by search query (case-insensitive)', () => {
      // 'ali' matches 'Alice' but not 'Andrew' or 'Bob'
      const wrapper = mountComponent({}, 'ali');

      const titles = wrapper
        .findAll('v-list-item')
        .map((li) => li.attributes('title'));

      expect(titles).toContain('Alice');
      expect(titles).not.toContain('Andrew');
      expect(titles).not.toContain('Bob');
    });

    it('hides groups whose items are all filtered out', () => {
      // 'bob' only matches Vancouver group
      const wrapper = mountComponent({}, 'bob');

      const headings = wrapper.findAll('h6').map((h) => h.text());
      expect(headings).not.toContain('Victoria');
      expect(headings).toContain('Vancouver');
    });

    it('shows no groups when search query matches nothing', () => {
      const wrapper = mountComponent({}, 'zzz-no-match');

      expect(wrapper.findAll('h6')).toHaveLength(0);
      expect(wrapper.findAll('v-list-item')).toHaveLength(0);
    });
  });

  describe('toggleItem', () => {
    it('emits update:modelValue with the item added when an unselected item is clicked', async () => {
      const wrapper = mountComponent({ modelValue: [] });

      const item = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Alice');
      await item!.trigger('click');

      expect(wrapper.emitted('update:modelValue')![0][0]).toEqual(['A1']);
    });

    it('emits update:modelValue with the item removed when a selected item is clicked', async () => {
      const wrapper = mountComponent({ modelValue: ['A1', 'B1'] });

      const item = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Alice');
      await item!.trigger('click');

      expect(wrapper.emitted('update:modelValue')![0][0]).toEqual(['B1']);
    });

    it('preserves other selections when toggling a single item', async () => {
      const wrapper = mountComponent({ modelValue: ['A2'] });

      const item = wrapper
        .findAll('v-list-item')
        .find((li) => li.attributes('title') === 'Bob');
      await item!.trigger('click');

      const emitted = wrapper.emitted('update:modelValue')![0][0] as string[];
      expect(emitted).toEqual(expect.arrayContaining(['A2', 'B1']));
    });
  });
});
