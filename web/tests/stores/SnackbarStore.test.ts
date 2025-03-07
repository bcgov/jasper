import { setActivePinia, createPinia } from 'pinia'
import { useSnackbarStore } from '@/stores/SnackbarStore';
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
    expect(store.timeout).toBe(undefined);
  });

  it('shows snackbar with given arguments', () => {
    store.showSnackbar('Test message', 'error', 'Test title', 20000);
    expect(store.isVisible).toBe(true);
    expect(store.message).toBe('Test message');
    expect(store.color).toBe('error');
    expect(store.title).toBe('Test title');
    expect(store.timeout).toBe(20000);
  });

  it('shows snackbar with default values when no arguments are passed', () => {
    store.showSnackbar();
    expect(store.isVisible).toBe(true);
    expect(store.message).toBe('');
    expect(store.color).toBe('success');
    expect(store.title).toBe('');
    expect(store.timeout).toBe(15000);
  });
});