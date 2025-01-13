
import { mount } from '@vue/test-utils';
import { useSnackbarStore } from '@/stores/SnackBarStore';
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
    const wrapper = mount(Snackbar);
    
    await wrapper.find('v-snackbar__actions v-icon').trigger('click');

    expect(store.isVisible).toBe(false);
  });
});