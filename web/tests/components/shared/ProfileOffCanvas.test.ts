import { mount } from '@vue/test-utils';
import { describe, it, expect, beforeEach, vi } from 'vitest';
import ProfileOffCanvas from 'CMP/shared/ProfileOffCanvas.vue';

vi.mock('SRC/stores/ThemeStore', () => ({
  useThemeStore: () => ({
    theme: 'light',
    setTheme: vi.fn(),
    changeState: vi.fn(),
  }),
}));

describe('ProfileOffCanvas.vue', () => {
  let wrapper;

  beforeEach(() => {
    wrapper = mount(ProfileOffCanvas);
  });

  it('renders the component', () => {
    expect(wrapper.exists()).toBe(true);
  });

  // Unable to dive deeper into slotted append/prepend components
  // todo: find a way to test the slotted components
  // it('calls close when close button clicked', async () => {
  //   await wrapper.find('v-button').trigger('click');

  //   expect(wrapper.emitted()).toHaveProperty('close');
  // });

  // it('calls set theme to dark when toggle button is clicked', async () => {
  //   await wrapper.find('v-switch').trigger('click');

  //   expect(themeStore.changeState).toHaveBeenCalledWith('dark');
  // });
});


