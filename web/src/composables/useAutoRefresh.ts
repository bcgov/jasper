import { useSnackbarStore } from '@/stores';
import { onUnmounted } from 'vue';

const TEN_MINUTES = 600000;
const NINE_MINUTES = 540000;
const ONE_MINUTE = 60000;

/**
 * Auto-refresh logic used by components that poll for updated data on a 10-minute interval and show a 1-minute warning beforehand.
 *
 * @param canRefresh  Returns true when a refresh is appropriate (e.g. a date is selected)
 * @param onRefresh   Called each time the refresh interval fires
 * @param isLoading   Returns true while a refresh is already in progress (suppresses the warning)
 */
export function useAutoRefresh(
  canRefresh: () => boolean,
  onRefresh: () => void | Promise<void>,
  isLoading: () => boolean
) {
  const snackBarStore = useSnackbarStore();
  let searchInterval: ReturnType<typeof setInterval>;
  let warningInterval: ReturnType<typeof setInterval>;
  let isWarningShown = false;

  const showWarning = () => {
    snackBarStore.showSnackbar(
      'This page will refresh in 1 minute to ensure you see the latest updates.',
      '#b4e6ff',
      '🔄 Heads-up!',
      ONE_MINUTE
    );
    isWarningShown = true;
  };

  const clearTimers = () => {
    clearInterval(searchInterval);
    clearInterval(warningInterval);
    if (isWarningShown) {
      snackBarStore.hideSnackbar();
      isWarningShown = false;
    }
  };

  const safeRefresh = async () => {
    try {
      await onRefresh();
    } catch (error) {
      console.error('Auto-refresh callback failed:', error);
    }
  };

  const setupAutoRefresh = () => {
    clearTimers();
    searchInterval = setInterval(() => {
      if (canRefresh() && !isLoading()) {
        safeRefresh();
      }
    }, TEN_MINUTES);
    warningInterval = setInterval(() => {
      if (canRefresh() && !isLoading()) {
        showWarning();
      }
    }, NINE_MINUTES);
  };

  onUnmounted(() => clearTimers());

  return { setupAutoRefresh };
}
