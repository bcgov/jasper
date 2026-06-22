import { GeneratePdfRequest } from '@/components/documents/models/GeneratePdf';
import { defineStore } from 'pinia';
import { v4 as uuidv4 } from 'uuid';

export const usePDFViewerStore = defineStore('PDFViewerStore', {
  persist: true,

  state: () => ({
    activeSessionId: null as string | null,
    sessions: {} as Record<string, StoreDocument[]>,
  }),

  getters: {
    documents: (state): StoreDocument[] => {
      if (!state.activeSessionId) {
        return [];
      }

      return state.sessions[state.activeSessionId] ?? [];
    },

    groupedDocuments: (
      state
    ): Record<string, Record<string, StoreDocument[]>> => {
      if (!state.activeSessionId) {
        return {};
      }

      return groupStoreDocuments(state.sessions[state.activeSessionId] ?? []);
    },

    getPdfItems:
      (state) =>
      (sessionId?: string): StoreDocument[] => {
        const resolvedSessionId = sessionId ?? state.activeSessionId;

        if (!resolvedSessionId) {
          return [];
        }

        return state.sessions[resolvedSessionId] ?? [];
      },

    hasPdfData:
      (state) =>
      (sessionId?: string): boolean => {
        const resolvedSessionId = sessionId ?? state.activeSessionId;

        if (!resolvedSessionId) {
          return false;
        }

        return (state.sessions[resolvedSessionId] ?? []).length > 0;
      },
  },

  actions: {
    setPdfItems(items: StoreDocument[], sessionId = uuidv4()): string {
      this.sessions[sessionId] = [...items];
      this.activeSessionId = sessionId;

      return sessionId;
    },

    addDocument(document: StoreDocument, sessionId?: string): string {
      const resolvedSessionId = sessionId ?? this.activeSessionId ?? uuidv4();

      if (!this.sessions[resolvedSessionId]) {
        this.sessions[resolvedSessionId] = [];
      }

      this.sessions[resolvedSessionId].push(document);
      this.activeSessionId = resolvedSessionId;

      return resolvedSessionId;
    },

    addDocuments(documents: StoreDocument[], sessionId = uuidv4()): string {
      this.sessions[sessionId] = [...documents];
      this.activeSessionId = sessionId;

      return sessionId;
    },

    clearPdfItems(sessionId?: string): void {
      const resolvedSessionId = sessionId ?? this.activeSessionId;

      if (!resolvedSessionId) {
        return;
      }

      delete this.sessions[resolvedSessionId];

      if (this.activeSessionId === resolvedSessionId) {
        this.activeSessionId = null;
      }
    },

    clearDocuments(sessionId?: string): void {
      this.clearPdfItems(sessionId);
    },

    clearAllSessions(): void {
      this.sessions = {};
      this.activeSessionId = null;
    },
  },
});

export type PdfOutlineSourceItem = {
  groupKeyOne: string;
  groupKeyTwo: string;
  documentName: string;
  physicalFileId?: string;
  participantId?: string;
  documentId?: string;
};

export type StoreDocument = PdfOutlineSourceItem & {
  request: GeneratePdfRequest;
  physicalFileId: string;
};

function groupStoreDocuments(
  documents: StoreDocument[]
): Record<string, Record<string, StoreDocument[]>> {
  return documents.reduce(
    (grouped, document) => {
      const groupKeyOne = document.groupKeyOne ?? '';
      const groupKeyTwo = document.groupKeyTwo ?? '';

      if (!grouped[groupKeyOne]) {
        grouped[groupKeyOne] = {};
      }

      if (!grouped[groupKeyOne][groupKeyTwo]) {
        grouped[groupKeyOne][groupKeyTwo] = [];
      }

      grouped[groupKeyOne][groupKeyTwo].push(document);

      return grouped;
    },
    {} as Record<string, Record<string, StoreDocument[]>>
  );
}
