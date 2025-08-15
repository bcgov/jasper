import { defineStore } from 'pinia';
import { GeneratePdfRequest } from '@/components/documents/models/GeneratePdf';

export const usePDFViewerStore = defineStore('PDFViewerStore', {
  persist: true,
  state: () => ({
    documents: [] as GeneratePdfRequest[],
  }),
  getters: {
    documentRequests: (state) => state.documents,
  },
  actions: {
    addDocuments(documents: GeneratePdfRequest[]): void {
      this.documents = [...documents];
    },
    clearDocuments(): void {
      this.documents.length = 0;
    },
  },
});
