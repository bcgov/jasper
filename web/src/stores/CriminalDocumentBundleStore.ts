import { defineStore } from 'pinia';
import { v4 as uuidv4 } from 'uuid';

export const useCriminalDocumentBundleStore = defineStore(
  'CriminalDocumentBundleStore',
  {
    persist: true,

    state: (): CriminalDocumentBundleStoreState => ({
      activeSessionId: null,
      sessions: {},
    }),

    getters: {
      getPdfItems:
        (state) =>
        (sessionId?: string): CriminalDocumentAppearanceRequest[] => {
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
      setPdfItems(
        items: CriminalDocumentAppearanceRequest[],
        sessionId = uuidv4()
      ): string {
        this.sessions[sessionId] = [...items];
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

      clearBundles(sessionId?: string): void {
        this.clearPdfItems(sessionId);
      },

      clearAllSessions(): void {
        this.sessions = {};
        this.activeSessionId = null;
      },
    },
  }
);

type CriminalDocumentBundleStoreState = {
  activeSessionId: string | null;
  sessions: Record<string, CriminalDocumentAppearanceRequest[]>;
};

export type CriminalDocumentAppearanceRequest = {
  appearance: AppearanceDocumentRequest;

  groupKeyOne: string;
  groupKeyTwo: string;
  documentName: string;

  fileNumber: string;
  fullName?: string;
  physicalFileId: string;
  participantId?: string;
};
