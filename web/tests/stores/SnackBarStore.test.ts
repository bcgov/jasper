import { setActivePinia, createPinia } from 'pinia'
import { useSnackbarStore } from '@/stores/SnackBarStore';
import { beforeEach, describe, expect, it } from 'vitest';

describe('SnackBarStore', () => {
  let store: ReturnType<typeof useSnackbarStore>;

  beforeEach(() => {
    setActivePinia(createPinia())
    store = useSnackbarStore();
  });

  it('initializes with default values', () => {
    expect(store.isVisible).toBe(false);
    expect(store.message).toBe('');
    expect(store.color).toBe('success');
    expect(store.title).toBe('');
  });

  it('shows snackbar with given message, color, and title', () => {
    store.showSnackbar('Test message', 'error', 'Test title');
    expect(store.isVisible).toBe(true);
    expect(store.message).toBe('Test message');
    expect(store.color).toBe('error');
    expect(store.title).toBe('Test title');
  });

  it('shows snackbar with default values when no arguments are passed', () => {
    store.showSnackbar();
    expect(store.isVisible).toBe(true);
    expect(store.message).toBe('');
    expect(store.color).toBe('success');
    expect(store.title).toBe('');
  });
});
