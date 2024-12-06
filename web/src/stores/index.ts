import { createPinia } from 'pinia';
import { App } from 'vue';

const pinia = createPinia();

export function registerPinia(app: App) {
  app.use(pinia);
}

export default pinia;

export { useCivilFileStore } from './CivilFileStore';
export { useCommonStore } from './CommonStore';
export { useCourtFileSearchStore } from './CourtFileSearchStore';
export { useCourtListStore } from './CourtListStore';
export { useCriminalFileStore } from './CriminalFileStore';
