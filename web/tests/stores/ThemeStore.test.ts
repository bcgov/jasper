import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { useThemeStore } from '@/stores/ThemeStore';

describe('ThemeStore', () => {
  let themeStore: ReturnType<typeof useThemeStore>;

  beforeEach(() => {
    themeStore = useThemeStore();
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('should initialize with light theme by default', () => {
    expect(themeStore.state.value).toBe('light');
  });

  it('should change theme and update localStorage', () => {
    themeStore.changeState('dark');
    expect(themeStore.state.value).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });

  it('should persist theme from localStorage', () => {
    localStorage.setItem('theme', 'dark');
    themeStore = useThemeStore();
    expect(themeStore.state.value).toBe('dark');
  });
});