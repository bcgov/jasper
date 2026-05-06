import Snackbar from '@/components/shared/Snackbar.vue';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, vi } from 'vitest';

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

  it('runs action handler and hides snackbar when action is clicked', async () => {
    const actionHandler = vi.fn();
    store.showSnackbar('Test message', 'error', 'Test title', 15000, {
      label: 'View package',
      onClick: actionHandler,
    });

    const wrapper = mount(Snackbar, {
      global: {
        stubs: {
          'v-snackbar': {
            template: '<div><slot /><slot name="actions" /></div>',
          },
          'v-btn': {
            template:
              '<button class="action-btn" @click="$emit(\'click\')"><slot /></button>',
          },
        },
      },
    });

    const actionButton = wrapper.find('.action-btn');
    expect(actionButton.exists()).toBe(true);

    await actionButton.trigger('click');

    expect(actionHandler).toHaveBeenCalledTimes(1);
    expect(store.isVisible).toBe(false);
    expect(store.actionLabel).toBe('');
    expect(store.actionHandler).toBeNull();
  });
});
