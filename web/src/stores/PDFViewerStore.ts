import { GeneratePdfRequest } from '@/components/documents/models/GeneratePdf';
import { defineStore } from 'pinia';

export const usePDFViewerStore = defineStore('PDFViewerStore', {
  persist: true,
  state: () => ({
    storedDocuments: [] as StoreDocument[],
    groupedDocuments: {} as Record<
      string,
      Record<string, StoreDocument[]>
    >,
  }),
  getters: {
    documents: (state) => state.storedDocuments,
  },
  actions: {
    addDocument(document: StoreDocument): void {
      this.storedDocuments.push(document);

      if (document.caseNumber) {
        // Group documents by caseNumber, then by memberName
        const grouped = this.documents.reduce(
          (acc, doc) => {
            if (!acc[doc.caseNumber]) acc[doc.caseNumber] = {};
            if (!acc[doc.caseNumber][doc.memberName])
              acc[doc.caseNumber][doc.memberName] = [];
            acc[doc.caseNumber][doc.memberName].push(doc);
            return acc;
          },
          {} as Record<string, Record<string, StoreDocument[]>>
        );
        this.groupedDocuments = grouped;
      }
    },
    clearDocuments(): void {
      this.storedDocuments.length = 0;
      this.groupedDocuments = {};
    },
  },
});

export type StoreDocument = {
  request: GeneratePdfRequest;
  caseNumber: string;
  memberName: string;
  documentName: string;
};
