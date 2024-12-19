import { mount } from '@vue/test-utils';
import { describe, it, expect } from 'vitest';
import ActionButtons from 'CMP/shared/Form/ActionButtons.vue';

describe('ActionButtons.vue', () => {
    it('renders search button when showSearch is true', () => {
      const wrapper = mount(ActionButtons, {
        props: { showSearch: true }
      });
      expect(wrapper.find('v-btn-tertiary').exists()).toBe(true);
    });
  
    it('does not render search button when showSearch is false', () => {
      const wrapper = mount(ActionButtons, {
        props: { showSearch: false }
      });
      expect(wrapper.find('v-btn-tertiary').exists()).toBe(false);
    });
  
    it('renders reset button when showReset is true', () => {
      const wrapper = mount(ActionButtons, {
        props: { showReset: true }
      });
      expect(wrapper.find('v-btn').exists()).toBe(true);
    });
  
    it('does not render reset button when showReset is false', () => {
      const wrapper = mount(ActionButtons, {
        props: { showReset: false }
      });
      expect(wrapper.find('v-btn').exists()).toBe(false);
    });
  
    it('emits "reset" event when reset button is clicked', async () => {
      const wrapper = mount(ActionButtons);
      await wrapper.find('v-btn').trigger('click');
      expect(wrapper.emitted()).toHaveProperty('reset');
    });
  });