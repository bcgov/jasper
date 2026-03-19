import FilterDropdownBase from '@/components/dashboard/court-calendar/filters/FilterDropdownBase.vue';
import { faker } from '@faker-js/faker';
import { mount } from '@vue/test-utils';
import { describe, expect, it } from 'vitest';
import { defineComponent, nextTick } from 'vue'; // nextTick needed for menu watcher flush

describe('FilterDropdownBase.vue', () => {
  // Stub v-menu so that both the #activator named slot and the default slot
  // are rendered in the DOM. Without Vuetify registered as a plugin, v-menu is
  // an unknown element that never processes named slots, so the activator
  // button (and badge inside it) would never appear.
  const vMenuStub = defineComponent({
    template: `<div><slot name="activator" :props="{}" /><slot /></div>`,
  });

  const mountComponent = (props = {}, slotContent = '') =>
    mount(FilterDropdownBase, {
      props: {
        title: faker.word.noun(),
        selectedCount: 0,
        ...props,
      },
      slots: {
        default: slotContent,
      },
      global: {
        stubs: { 'v-menu': vMenuStub },
      },
    });

  describe('Activator button', () => {
    it('renders the title text in the activator button', () => {
      const title = faker.word.noun();
      const wrapper = mountComponent({ title });

      expect(wrapper.find('v-btn').text()).toContain(title);
    });

    it('displays a badge when selectedCount is greater than 0', () => {
      const wrapper = mountComponent({ selectedCount: 3 });

      const badge = wrapper.find('v-badge');
      expect(badge.exists()).toBe(true);
      expect(badge.attributes('content')).toBe('3');
    });

    it('does not display a badge when selectedCount is 0', () => {
      const wrapper = mountComponent({ selectedCount: 0 });

      expect(wrapper.find('v-badge').exists()).toBe(false);
    });
  });

  describe('Menu behaviour', () => {
    it('menu is closed by default', () => {
      const wrapper = mountComponent();

      expect((wrapper.vm as any).menu).toBe(false);
    });

    it('emits "open" when menu opens', async () => {
      const wrapper = mountComponent();

      (wrapper.vm as any).menu = true;
      await nextTick();

      expect(wrapper.emitted('open')).toBeTruthy();
    });

    it('emits "close" when menu closes after being opened', async () => {
      const wrapper = mountComponent();

      (wrapper.vm as any).menu = true;
      await nextTick();

      (wrapper.vm as any).menu = false;
      await nextTick();

      expect(wrapper.emitted('close')).toBeTruthy();
    });
  });

  describe('Search field', () => {
    it('renders search field when showSearch is true', () => {
      const wrapper = mountComponent({ showSearch: true });

      expect(wrapper.find('.search-field').exists()).toBe(true);
    });

    it('does not render search field when showSearch is false', () => {
      const wrapper = mountComponent({ showSearch: false });

      expect(wrapper.find('.search-field').exists()).toBe(false);
    });

    it('does not render search field when showSearch is undefined (default)', () => {
      const wrapper = mountComponent();

      // showSearch is undefined by default so v-if="showSearch" is falsy
      expect(wrapper.find('.search-field').exists()).toBe(false);
    });

    it('renders search placeholder with lowercase title', () => {
      const wrapper = mountComponent({ title: 'Locations', showSearch: true });

      const searchField = wrapper.find('.search-field');
      expect(searchField.attributes('placeholder')).toBe(
        'Search locations...'
      );
    });
  });

  describe('Slot content', () => {
    it('renders slot content', () => {
      const wrapper = mountComponent(
        {},
        '<div data-testid="slot-content">Hello</div>'
      );

      expect(wrapper.find('[data-testid="slot-content"]').exists()).toBe(true);
      expect(wrapper.find('[data-testid="slot-content"]').text()).toBe('Hello');
    });
  });
});
