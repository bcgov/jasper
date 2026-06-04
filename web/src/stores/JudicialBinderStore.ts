import { defineStore } from 'pinia';
import { v4 as uuidv4 } from 'uuid';

export const useJudicialBinderStore = defineStore('JudicialBinderStore', {
  persist: true,

  state: () => ({
    activeSessionId: null as string | null,
    sessions: {} as Record<string, JudicialBinderDocumentRequest[]>,
  }),

  getters: {
    getPdfItems:
      (state) =>
      (sessionId?: string): JudicialBinderDocumentRequest[] => {
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

    getBundles: (state): JudicialBinderBundle[] => {
      return Object.entries(state.sessions).map(([id, binders]) => ({
        id,
        binders,
      }));
    },

    getBundle:
      (state) =>
      (id: string): JudicialBinderBundle | undefined => {
        const binders = state.sessions[id];

        if (!binders) {
          return undefined;
        }

        return {
          id,
          binders,
        };
      },
  },

  actions: {
    setPdfItems(
      items: JudicialBinderDocumentRequest[],
      sessionId = uuidv4()
    ): string {
      this.sessions[sessionId] = [...items];
      this.activeSessionId = sessionId;

      return sessionId;
    },

    addBundle(bundle: JudicialBinderBundle): string {
      this.sessions[bundle.id] = [...bundle.binders];
      this.activeSessionId = bundle.id;

      return bundle.id;
    },

    addBinder(
      binder: JudicialBinderDocumentRequest,
      sessionId?: string
    ): string {
      const resolvedSessionId = sessionId ?? this.activeSessionId;

      if (!resolvedSessionId || !this.sessions[resolvedSessionId]) {
        return resolvedSessionId ?? '';
      }

      this.sessions[resolvedSessionId].push(binder);
      this.activeSessionId = resolvedSessionId;

      return resolvedSessionId;
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

    clearBundles(sessionId?: string): void {
      this.clearPdfItems(sessionId);
    },

    clearAllSessions(): void {
      this.sessions = {};
      this.activeSessionId = null;
    },
  },
});

export type JudicialBinderBundle = {
  id: string;
  binders: JudicialBinderDocumentRequest[];
};

export type JudicialBinderDocumentRequest = {
  labels: Record<string, string>;
  fileNumber: string;
  groupKeyOne: string;
  groupKeyTwo: string;
  physicalFileId: string;
  participantId: string;
  documentName: string;
  documentId: string;
};
