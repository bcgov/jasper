import { useCommonStore } from '@/stores';
import { mount } from '@vue/test-utils';
import ProfileOffCanvas from 'CMP/shared/ProfileOffCanvas.vue';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('SRC/stores/ThemeStore', () => ({
  useThemeStore: () => ({
    state: 'light',
    theme: 'light',
    setTheme: vi.fn(),
    changeState: vi.fn(),
  }),
}));

describe('ProfileOffCanvas.vue', () => {
  let wrapper;
  let pinia: any;

  beforeEach(() => {
    pinia = createPinia();
    setActivePinia(pinia);

    const commonStore = useCommonStore();
    commonStore.userInfo = { userTitle: 'Judge Josh', judgeId: 123 } as any;
    commonStore.loggedInUserInfo = {
      userTitle: 'Judge Josh',
      judgeId: 123,
    } as any;

    // Mount with explicit props to bypass store dependency
    wrapper = mount(ProfileOffCanvas, {
      global: {
        plugins: [pinia],
        provide: {
          // Provide mock data directly if needed
        },
      },
    });
  });

  it('renders the component', () => {
    expect(wrapper.exists()).toBe(true);
  });

  it('displays user name from store', () => {
    expect(wrapper.html()).toContain('Judge Josh');
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
