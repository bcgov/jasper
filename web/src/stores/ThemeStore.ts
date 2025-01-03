import { ref } from 'vue';

// We want the default experience to be light mode
const theme = localStorage.getItem('theme') ?? 'light';
const state = ref(theme);

const changeState = (newTheme: string) => {
  localStorage.setItem('theme', newTheme);
  state.value = newTheme;
};

export const useThemeStore = () => {
  return { state, changeState };
};
