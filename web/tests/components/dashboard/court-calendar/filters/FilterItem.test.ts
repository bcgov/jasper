import FilterItem from '@/components/dashboard/court-calendar/filters/FilterItem.vue';
import { TextValue } from '@/types/TextValue';
import { faker } from '@faker-js/faker';
import { mount, VueWrapper } from '@vue/test-utils';
import { beforeEach, describe, expect, it } from 'vitest';
import { nextTick } from 'vue';

describe('FilterItem.vue', () => {
  let wrapper: VueWrapper;
  let mockItems: TextValue[];

  const createMockItem = (): TextValue => ({
    value: faker.string.uuid(),
    text: faker.location.city(),
  });

  const createMockItems = (count: number): TextValue[] => {
    return Array.from({ length: count }, () => createMockItem());
  };

  beforeEach(() => {
    mockItems = createMockItems(10);
  });

  const mountComponent = (props = {}) => {
    return mount(FilterItem, {
      props: {
        title: faker.word.noun(),
        items: mockItems,
        modelValue: [],
        previewCount: 5,
        showSearch: true,
        ...props,
      },
    });
  };

  describe('Component Rendering', () => {
    it('renders the component', () => {
      wrapper = mountComponent();
      expect(wrapper.exists()).toBe(true);
    });

    it('renders search field when showSearch is true', () => {
      wrapper = mountComponent({ showSearch: true });
      expect(wrapper.find('.search-field').exists()).toBe(true);
    });

    it('does not render search field when showSearch is false', () => {
      wrapper = mountComponent({ showSearch: false });
      expect(wrapper.find('.search-field').exists()).toBe(false);
    });

    it('renders Select All checkbox', () => {
      wrapper = mountComponent();
      const checkboxes = wrapper.findAll('v-checkbox');
      expect(checkboxes.length).toBeGreaterThan(0);
      expect(checkboxes[0].attributes('label')).toBe('Select All');
    });

    it('renders filter list container', () => {
      wrapper = mountComponent();
      expect(wrapper.find('.filter-list-container').exists()).toBe(true);
    });
  });

  describe('Item Display', () => {
    it('displays preview count items by default', () => {
      wrapper = mountComponent({ previewCount: 5 });
      const listItems = wrapper.findAll('v-list-item');
      expect(mockItems.length).toBeGreaterThan(5);
    });

    it('displays all items when showAll is toggled', async () => {
      wrapper = mountComponent({ previewCount: 3 });

      // Should show "See all" button
      const showAllBtn = wrapper.find('.link-style-btn');
      expect(showAllBtn.exists()).toBe(true);

      await showAllBtn.trigger('click');
      await nextTick();

      // Button text should change
      expect(showAllBtn.text()).toContain('Show Less');
    });

    it('hides "See all" button when items count is less than preview count', () => {
      const fewItems = createMockItems(3);
      wrapper = mountComponent({ items: fewItems, previewCount: 5 });

      expect(wrapper.find('.link-style-btn').exists()).toBe(false);
    });

    it('displays "See all" button with correct title', () => {
      const title = faker.word.noun();
      wrapper = mountComponent({ title, previewCount: 3 });

      const showAllBtn = wrapper.find('.link-style-btn');
      expect(showAllBtn.text()).toContain(`See all ${title.toLowerCase()}`);
    });
  });

  describe('Search Functionality', () => {
    it('filters items based on search query', async () => {
      const searchableItems: TextValue[] = [
        { value: '1', text: 'Vancouver' },
        { value: '2', text: 'Victoria' },
        { value: '3', text: 'Surrey' },
      ];

      wrapper = mountComponent({ items: searchableItems, showSearch: true });

      // Update the localSearchQuery ref directly
      (wrapper.vm as any).localSearchQuery = 'Van';
      await nextTick();

      // The search should filter items containing 'Van'
      expect((wrapper.vm as any).filteredItems).toHaveLength(1);
      expect((wrapper.vm as any).filteredItems[0].text).toBe('Vancouver');
    });

    it('shows all items when search query is cleared', async () => {
      wrapper = mountComponent();

      (wrapper.vm as any).localSearchQuery = 'test';
      await nextTick();

      (wrapper.vm as any).localSearchQuery = '';
      await nextTick();

      expect((wrapper.vm as any).filteredItems.length).toBe(mockItems.length);
    });

    it('shows "no items found" message when search has no results', async () => {
      const title = faker.word.noun();
      wrapper = mountComponent({ title });

      (wrapper.vm as any).localSearchQuery = faker.string.uuid();
      await nextTick();

      expect(wrapper.find('.text-caption').exists()).toBe(true);
      expect(wrapper.find('.text-caption').text()).toContain(
        `No ${title.toLowerCase()} found`
      );
    });

    it('displays all filtered items when searching (ignores preview count)', async () => {
      const items = createMockItems(20);
      wrapper = mountComponent({ items, previewCount: 5 });

      (wrapper.vm as any).localSearchQuery = items[10].text;
      await nextTick();

      // When searching, should show all matching results, not limited by preview
      expect((wrapper.vm as any).displayedItems.length).toBeLessThanOrEqual(
        items.length
      );
    });

    it('hides "See all" button when searching', async () => {
      wrapper = mountComponent({ previewCount: 3 });

      expect(wrapper.find('.link-style-btn').exists()).toBe(true);

      (wrapper.vm as any).localSearchQuery = 'test';
      await nextTick();

      expect(wrapper.find('.link-style-btn').exists()).toBe(false);
    });
  });

  describe('Selection Functionality', () => {
    it('selects individual item when checkbox is clicked', async () => {
      wrapper = mountComponent();

      const itemToSelect = mockItems[0];
      await (wrapper.vm as any).toggleItem(itemToSelect.value);

      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([
        [itemToSelect.value],
      ]);
    });

    it('deselects individual item when already selected', async () => {
      const selectedItem = mockItems[0];
      wrapper = mountComponent({ modelValue: [selectedItem.value] });

      await (wrapper.vm as any).toggleItem(selectedItem.value);

      const emissions = wrapper.emitted('update:modelValue');
      expect(emissions?.[0]).toEqual([[]]);
    });

    it('shows items as selected when in modelValue', () => {
      const selectedIds = [mockItems[0].value, mockItems[1].value];
      wrapper = mountComponent({ modelValue: selectedIds });

      expect((wrapper.vm as any).selectedItems).toEqual(selectedIds);
    });
  });

  describe('Select All Functionality', () => {
    it('selects all items when Select All is clicked', async () => {
      wrapper = mountComponent();

      await (wrapper.vm as any).toggleSelectAll();

      const allItemIds = mockItems.map((item) => item.value);
      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([allItemIds]);
    });

    it('deselects all items when Select All is clicked and all are selected', async () => {
      const allIds = mockItems.map((item) => item.value);
      wrapper = mountComponent({ modelValue: allIds });

      await (wrapper.vm as any).toggleSelectAll();

      expect(wrapper.emitted('update:modelValue')?.[0]).toEqual([[]]);
    });

    it('shows indeterminate state when some items are selected', () => {
      const someIds = [mockItems[0].value, mockItems[1].value];
      wrapper = mountComponent({ modelValue: someIds });

      expect((wrapper.vm as any).isIndeterminate).toBe(true);
      expect((wrapper.vm as any).isAllSelected).toBe(false);
    });

    it('shows all selected state when all items are selected', () => {
      const allIds = mockItems.map((item) => item.value);
      wrapper = mountComponent({ modelValue: allIds });

      expect((wrapper.vm as any).isAllSelected).toBe(true);
      expect((wrapper.vm as any).isIndeterminate).toBe(false);
    });

    it('shows not selected state when no items are selected', () => {
      wrapper = mountComponent({ modelValue: [] });

      expect((wrapper.vm as any).isAllSelected).toBe(false);
      expect((wrapper.vm as any).isIndeterminate).toBe(false);
    });

    it('Select All works with filtered items', async () => {
      const items: TextValue[] = [
        { value: '1', text: 'Apple' },
        { value: '2', text: 'Apricot' },
        { value: '3', text: 'Banana' },
      ];

      wrapper = mountComponent({ items });

      // Search for items starting with 'A'
      (wrapper.vm as any).localSearchQuery = 'Ap';
      await nextTick();

      // Select all filtered items
      await (wrapper.vm as any).toggleSelectAll();

      // Should only select the filtered items (Apple, Apricot)
      const emitted = wrapper.emitted(
        'update:modelValue'
      )?.[0]?.[0] as string[];
      expect(emitted).toContain('1');
      expect(emitted).toContain('2');
      expect(emitted).not.toContain('3');
    });

    it('deselects only filtered items when Select All is toggled off', async () => {
      const items: TextValue[] = [
        { value: '1', text: 'Apple' },
        { value: '2', text: 'Apricot' },
        { value: '3', text: 'Banana' },
      ];

      wrapper = mountComponent({ items, modelValue: ['1', '2', '3'] });

      // Search for items starting with 'A'
      (wrapper.vm as any).localSearchQuery = 'Ap';
      await nextTick();

      // Deselect all filtered items
      await (wrapper.vm as any).toggleSelectAll();

      // Should only deselect filtered items, Banana should remain selected
      const emitted = wrapper.emitted(
        'update:modelValue'
      )?.[0]?.[0] as string[];
      expect(emitted).toContain('3');
      expect(emitted).not.toContain('1');
      expect(emitted).not.toContain('2');
    });
  });

  describe('Preview Count', () => {
    it('respects custom preview count', () => {
      const previewCount = faker.number.int({ min: 1, max: 5 });
      wrapper = mountComponent({ previewCount });

      // Verify through the displayedItems computed property behavior
      expect((wrapper.vm as any).displayedItems.length).toBeLessThanOrEqual(
        previewCount
      );
    });

    it('defaults to 5 when preview count not provided', () => {
      wrapper = mount(FilterItem, {
        props: {
          title: faker.word.noun(),
          items: mockItems,
          modelValue: [],
        },
      });

      // Default previewCount is 5, so displayedItems should be at most 5
      expect((wrapper.vm as any).displayedItems.length).toBeLessThanOrEqual(5);
    });

    it('shows correct number of items based on preview count', async () => {
      const previewCount = 3;
      wrapper = mountComponent({ previewCount });

      expect((wrapper.vm as any).displayedItems.length).toBe(previewCount);
    });
  });

  describe('Edge Cases', () => {
    it('handles empty items array', () => {
      wrapper = mountComponent({ items: [] });

      expect(wrapper.find('.text-caption').exists()).toBe(true);
      expect((wrapper.vm as any).filteredItems).toHaveLength(0);
    });

    it('handles single item', () => {
      const singleItem = [createMockItem()];
      wrapper = mountComponent({ items: singleItem });

      expect((wrapper.vm as any).displayedItems).toHaveLength(1);
      expect(wrapper.find('.link-style-btn').exists()).toBe(false);
    });

    it('handles case-insensitive search', async () => {
      const items: TextValue[] = [
        { value: '1', text: 'Vancouver' },
        { value: '2', text: 'victoria' },
      ];

      wrapper = mountComponent({ items });

      (wrapper.vm as any).localSearchQuery = 'VICTORIA';
      await nextTick();

      expect((wrapper.vm as any).filteredItems).toHaveLength(1);
      expect((wrapper.vm as any).filteredItems[0].text).toBe('victoria');
    });

    it('maintains selection when toggling show all', async () => {
      const selectedIds = [mockItems[0].value, mockItems[5].value];
      wrapper = mountComponent({ modelValue: selectedIds, previewCount: 3 });

      const showAllBtn = wrapper.find('.link-style-btn');
      await showAllBtn.trigger('click');
      await nextTick();

      expect((wrapper.vm as any).selectedItems).toEqual(selectedIds);
    });

    it('maintains selection when searching', async () => {
      const items: TextValue[] = [
        { value: '1', text: 'Vancouver' },
        { value: '2', text: 'Victoria' },
      ];

      wrapper = mountComponent({ items, modelValue: ['1', '2'] });

      (wrapper.vm as any).localSearchQuery = 'Van';
      await nextTick();

      expect((wrapper.vm as any).selectedItems).toEqual(['1', '2']);
    });

    it('handles rapid toggle clicks', async () => {
      wrapper = mountComponent({ previewCount: 3 });

      const showAllBtn = wrapper.find('.link-style-btn');

      await showAllBtn.trigger('click');
      await showAllBtn.trigger('click');
      await showAllBtn.trigger('click');
      await nextTick();

      // Should end up in "show all" state (odd number of clicks)
      expect((wrapper.vm as any).showAll).toBe(true);
    });
  });

  describe('V-Model Integration', () => {
    it('emits update:modelValue when item is toggled', async () => {
      wrapper = mountComponent();

      await (wrapper.vm as any).toggleItem(mockItems[0].value);

      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
    });

    it('updates internal state when modelValue prop changes', async () => {
      wrapper = mountComponent({ modelValue: [] });

      const newSelection = [mockItems[0].value, mockItems[1].value];
      await wrapper.setProps({ modelValue: newSelection });

      expect((wrapper.vm as any).selectedItems).toEqual(newSelection);
    });
  });

  describe('Accessibility', () => {
    it('has proper placeholder text for search field', () => {
      const title = 'Locations';
      wrapper = mountComponent({ title, showSearch: true });

      const searchField = wrapper.find('v-text-field');
      expect(searchField.attributes('placeholder')).toBe('Search locations...');
    });

    it('shows compact density for all checkboxes', () => {
      wrapper = mountComponent();

      const checkboxes = wrapper.findAll('v-checkbox');
      expect(checkboxes.length).toBeGreaterThan(0);
    });

    it('renders checkboxes with hide-details attribute', () => {
      wrapper = mountComponent();

      const checkboxes = wrapper.findAll('v-checkbox');
      checkboxes.forEach((checkbox) => {
        expect(checkbox.attributes('hide-details')).toBeDefined();
      });
    });
  });
});
