import { defineStore } from 'pinia';
import { ref } from 'vue';

export const useSnackbarStore = defineStore('snackbar', () => {
  const isVisible = ref(false);
  const message = ref('');
  const color = ref('success');
  const title = ref('');

  const showSnackbar = (msg = '', col = 'success', ti = '') => {
    message.value = msg;
    color.value = col;
    title.value = ti;
    isVisible.value = true;
  };

  return { isVisible, message, color, showSnackbar, title };
});
