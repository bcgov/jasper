import { mount } from '@vue/test-utils';
import ActivityClassFilter from 'CMP/dashboard/court-calendar/filters/ActivityClassFilter.vue';
import { describe, expect, it } from 'vitest';
import { defineComponent } from 'vue';

// Without Vuetify registered as a plugin, v-btn-toggle is an unknown element
// and findComponent cannot return a vm instance for it. Stubbing it with a
// real defineComponent gives us a resolvable Vue instance to emit events from.
const vBtnToggleStub = defineComponent({
  name: 'VBtnToggle',
  props: ['modelValue'],
  emits: ['update:modelValue'],
  template: '<div><slot /></div>',
});

describe('ActivityClassFilter.vue', () => {
  const mountComponent = (props = {}) =>
    mount(ActivityClassFilter, {
      props: {
        modelValue: 'all',
        ...props,
      },
      global: {
        stubs: { 'v-btn-toggle': vBtnToggleStub },
      },
    });

  describe('rendering', () => {
    it('renders the component', () => {
      const wrapper = mountComponent();
      expect(wrapper.exists()).toBe(true);
    });

    it('renders the All button', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('All');
    });

    it('renders the Sitting button', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('Sitting');
    });

    it('renders the Non-sitting button', () => {
      const wrapper = mountComponent();
      expect(wrapper.text()).toContain('Non-sitting');
    });

    it('renders three buttons', () => {
      const wrapper = mountComponent();
      expect(wrapper.findAll('v-btn').length).toBe(3);
    });

    it('renders All button with value "all"', () => {
      const wrapper = mountComponent();
      expect(wrapper.find('[value="all"]').exists()).toBe(true);
    });

    it('renders Sitting button with value "SIT"', () => {
      const wrapper = mountComponent();
      expect(wrapper.find('[value="SIT"]').exists()).toBe(true);
    });

    it('renders Non-sitting button with value "NS"', () => {
      const wrapper = mountComponent();
      expect(wrapper.find('[value="NS"]').exists()).toBe(true);
    });
  });

  describe('default prop', () => {
    it('defaults modelValue to "all" when not provided', () => {
      const wrapper = mount(ActivityClassFilter, {
        global: { stubs: { 'v-btn-toggle': vBtnToggleStub } },
      });
      expect(wrapper.props()['modelValue']).toBe('all');
    });
  });

  describe('props', () => {
    it('accepts "all" as modelValue', () => {
      const wrapper = mountComponent({ modelValue: 'all' });
      expect(wrapper.props()['modelValue']).toBe('all');
    });

    it('accepts "SIT" as modelValue', () => {
      const wrapper = mountComponent({ modelValue: 'SIT' });
      expect(wrapper.props()['modelValue']).toBe('SIT');
    });

    it('accepts "NS" as modelValue', () => {
      const wrapper = mountComponent({ modelValue: 'NS' });
      expect(wrapper.props()['modelValue']).toBe('NS');
    });
  });

  describe('emits', () => {
    it('emits "update:modelValue" with "SIT" when toggle changes to Sitting', async () => {
      const wrapper = mountComponent({ modelValue: 'all' });
      await wrapper
        .findComponent(vBtnToggleStub)
        .vm.$emit('update:modelValue', 'SIT');
      expect(wrapper.emitted('update:modelValue')).toBeTruthy();
      expect(wrapper.emitted('update:modelValue')![0]).toEqual(['SIT']);
    });

    it('emits "update:modelValue" with "NS" when toggle changes to Non-sitting', async () => {
      const wrapper = mountComponent({ modelValue: 'all' });
      await wrapper
        .findComponent(vBtnToggleStub)
        .vm.$emit('update:modelValue', 'NS');
      expect(wrapper.emitted('update:modelValue')![0]).toEqual(['NS']);
    });

    it('emits "update:modelValue" with "all" when toggle changes to All', async () => {
      const wrapper = mountComponent({ modelValue: 'SIT' });
      await wrapper
        .findComponent(vBtnToggleStub)
        .vm.$emit('update:modelValue', 'all');
      expect(wrapper.emitted('update:modelValue')![0]).toEqual(['all']);
    });
  });
});
