import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useSnackbarStore = defineStore('snackbar', () => {
  const isVisible = ref(false);
  const message = ref('');
  const color = ref('success');
  const title = ref('');
  const timeout = ref<number>();

  const showSnackbar = (msg = '', col = 'success', ti = '', time = 15000) => {
    message.value = msg;
    color.value = col;
    title.value = ti;
    isVisible.value = true;
    timeout.value = time;
  };

  const hideSnackbar = () => {
    isVisible.value = false;
  };

  return {
    isVisible,
    message,
    color,
    showSnackbar,
    hideSnackbar,
    title,
    timeout,
  };
});
