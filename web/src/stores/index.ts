import { createPinia } from 'pinia';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate';
import { App } from 'vue';

const pinia = createPinia();
pinia.use(piniaPluginPersistedstate);

export function registerPinia(app: App) {
  app.use(pinia);
}

export default pinia;

export { useBundleStore } from './BundleStore';
export { useCivilFileStore } from './CivilFileStore';
export { useCommonStore } from './CommonStore';
export { useCourtFileSearchStore } from './CourtFileSearchStore';
export { useCourtListStore } from './CourtListStore';
export { useCriminalFileStore } from './CriminalFileStore';
export { useDarsStore } from './DarsStore';
export { usePDFViewerStore } from './PDFViewerStore';
export { useSnackbarStore } from './SnackbarStore';
