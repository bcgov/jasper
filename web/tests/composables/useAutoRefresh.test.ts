import { useAutoRefresh } from '@/composables/useAutoRefresh';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { mount } from '@vue/test-utils';
import { createPinia, setActivePinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, Mock, vi } from 'vitest';
import { defineComponent, h } from 'vue';

const TEN_MINUTES = 600000;
const NINE_MINUTES = 540000;
const ONE_MINUTE = 60000;

/**
 * Mounts the composable inside a real component so that Vue lifecycle
 * hooks (onUnmounted) are properly registered and called.
 */
function withSetup(composable: () => ReturnType<typeof useAutoRefresh>) {
  let result: ReturnType<typeof useAutoRefresh>;
  const TestComponent = defineComponent({
    setup() {
      result = composable();
      return () => h('div');
    },
  });
  const wrapper = mount(TestComponent);
  return { result: result!, wrapper };
}

describe('useAutoRefresh', () => {
  let snackbarStore: ReturnType<typeof useSnackbarStore>;
  let canRefresh: Mock<() => boolean>;
  let onRefresh: Mock<() => void>;
  let isLoading: Mock<() => boolean>;

  beforeEach(() => {
    vi.useFakeTimers();
    setActivePinia(createPinia());
    snackbarStore = useSnackbarStore();
    canRefresh = vi.fn().mockReturnValue(true);
    onRefresh = vi.fn();
    isLoading = vi.fn().mockReturnValue(false);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  describe('setupAutoRefresh', () => {
    it('calls onRefresh after 10 minutes when canRefresh is true', () => {
      const { result } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      vi.advanceTimersByTime(TEN_MINUTES);

      expect(onRefresh).toHaveBeenCalledOnce();
    });

    it('does not call onRefresh when canRefresh is false', () => {
      canRefresh.mockReturnValue(false);
      const { result } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      vi.advanceTimersByTime(TEN_MINUTES);

      expect(onRefresh).not.toHaveBeenCalled();
    });

    it('shows warning snackbar after 9 minutes when canRefresh is true and not loading', () => {
      const { result } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      vi.advanceTimersByTime(NINE_MINUTES);

      expect(snackbarStore.isVisible).toBe(true);
      expect(snackbarStore.message).toBe(
        'This page will refresh in 1 minute to ensure you see the latest updates.'
      );
      expect(snackbarStore.color).toBe('#b4e6ff');
      expect(snackbarStore.title).toBe('🔄 Heads-up!');
      expect(snackbarStore.timeout).toBe(ONE_MINUTE);
    });

    it('does not show warning when isLoading is true', () => {
      isLoading.mockReturnValue(true);
      const { result } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      vi.advanceTimersByTime(NINE_MINUTES);

      expect(snackbarStore.isVisible).toBe(false);
    });

    it('does not show warning when canRefresh is false', () => {
      canRefresh.mockReturnValue(false);
      const { result } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      vi.advanceTimersByTime(NINE_MINUTES);

      expect(snackbarStore.isVisible).toBe(false);
    });

    it('resets timers when called again before the interval fires', () => {
      const { result } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      vi.advanceTimersByTime(TEN_MINUTES / 2); // 5 min in

      result.setupAutoRefresh(); // reset — old interval is cleared
      vi.advanceTimersByTime(TEN_MINUTES / 2); // only 5 min since reset

      expect(onRefresh).not.toHaveBeenCalled();

      vi.advanceTimersByTime(TEN_MINUTES / 2); // 10 min since reset

      expect(onRefresh).toHaveBeenCalledOnce();
    });
  });

  describe('onUnmounted cleanup', () => {
    it('stops calling onRefresh after the component is unmounted', () => {
      const { result, wrapper } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      wrapper.unmount();

      vi.advanceTimersByTime(TEN_MINUTES);

      expect(onRefresh).not.toHaveBeenCalled();
    });

    it('hides the snackbar when the component is unmounted', () => {
      const { result, wrapper } = withSetup(() =>
        useAutoRefresh(canRefresh, onRefresh, isLoading)
      );

      result.setupAutoRefresh();
      vi.advanceTimersByTime(NINE_MINUTES); // trigger warning
      expect(snackbarStore.isVisible).toBe(true);

      wrapper.unmount();

      expect(snackbarStore.isVisible).toBe(false);
    });
  });
});
