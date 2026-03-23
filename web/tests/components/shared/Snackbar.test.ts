import { mount } from '@vue/test-utils';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { describe, it, expect, beforeEach } from 'vitest';
import Snackbar from '@/components/shared/Snackbar.vue';
import { setActivePinia, createPinia } from 'pinia'

describe('Snackbar.vue', () => {
    let store: ReturnType<typeof useSnackbarStore>;

    beforeEach(() => {
        setActivePinia(createPinia());
        store = useSnackbarStore();
    });

  it('renders snackbar with correct props', () => {
    store.showSnackbar('Test message', 'error', 'Test title');
    const wrapper = mount(Snackbar);

    expect(wrapper.find('h3').text()).toBe('Test title');
    expect(wrapper.text()).toContain('Test message');
    expect(store.isVisible).toBe(true);
  });

  it('closes snackbar when close button is clicked', async () => {
    store.showSnackbar('Test message', 'error', 'Test title');
    const wrapper = mount(Snackbar, {
      global: {
        stubs: {
          'v-snackbar': {
            template: '<div><slot /><slot name="actions" /></div>',
          },
        },
      },
    });

    const closeButton = wrapper.find('v-icon');
    expect(closeButton.exists()).toBe(true);

    await closeButton.trigger('click');

    expect(store.isVisible).toBe(false);
  });
});