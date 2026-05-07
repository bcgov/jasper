import { defineStore } from 'pinia';
import { ref } from 'vue';

export type SnackbarAction = {
  label: string;
  onClick: () => void;
};

export const useSnackbarStore = defineStore('snackbar', () => {
  const isVisible = ref(false);
  const message = ref('');
  const color = ref('success');
  const title = ref('');
  const timeout = ref<number>();
  const actionLabel = ref('');
  const actionHandler = ref<(() => void) | null>(null);

  const showSnackbar = (
    msg = '',
    col = 'success',
    ti = '',
    time = 15000,
    action?: SnackbarAction
  ) => {
    message.value = msg;
    color.value = col;
    title.value = ti;
    isVisible.value = true;
    timeout.value = time;
    actionLabel.value = action?.label ?? '';
    actionHandler.value = action?.onClick ?? null;
  };

  const hideSnackbar = () => {
    isVisible.value = false;
    actionLabel.value = '';
    actionHandler.value = null;
  };

  return {
    isVisible,
    message,
    color,
    showSnackbar,
    hideSnackbar,
    title,
    timeout,
    actionLabel,
    actionHandler,
  };
});
