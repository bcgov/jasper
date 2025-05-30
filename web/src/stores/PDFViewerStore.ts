import { defineStore } from 'pinia';

export const usePDFViewerStore = defineStore('PDFViewerStore', {
  persist: true,
  state: () => ({
    urls: [] as string[]
  }),
  getters: {
    documentUrls: (state) => state.urls,
  },
  actions: {
    addUrls({ urls }): void {
      this.urls = [...urls];
    },
    clearUrls(): void {
      this.urls.length = 0;
    },
  },
});
