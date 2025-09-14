import { createPinia } from 'pinia';
import { App } from 'vue';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate'

const pinia = createPinia();
pinia.use(piniaPluginPersistedstate)

export function registerPinia(app: App) {
  app.use(pinia);
}

export default pinia;

export { useCivilFileStore } from './CivilFileStore';
export { useCommonStore } from './CommonStore';
export { useCourtFileSearchStore } from './CourtFileSearchStore';
export { useCourtListStore } from './CourtListStore';
export { useCriminalFileStore } from './CriminalFileStore';
export { useSnackbarStore } from './SnackbarStore';
export { usePDFViewerStore } from './PDFViewerStore';
export { useBundleStore } from './BundleStore';