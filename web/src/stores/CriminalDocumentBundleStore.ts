import { defineStore } from 'pinia';
import { Binder } from '@/types/Binder';
import { CriminalDocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { AppearanceDocumentRequest } from '@/types/AppearanceDocumentRequest';
import { v4 as uuidv4 } from 'uuid';

export const useCriminalDocumentBundleStore = defineStore(
  'CriminalDocumentBundleStore',
  {
    persist: true,

    state: (): CriminalDocumentBundleStoreState => ({
      activeSessionId: null,
      sessions: {},

      // Kept for compatibility with existing callers during migration.
      bundles: [],
      request: { appearances: [] },
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

      getAppearanceRequests: (state): CriminalDocumentAppearanceRequest[] => {
        if (!state.activeSessionId) {
          return [];
        }

        return state.sessions[state.activeSessionId] ?? [];
      },

      getRequests: (state): CriminalDocumentBundleRequest => {
        if (!state.activeSessionId) {
          return state.request;
        }

        return {
          appearances:
            state.sessions[state.activeSessionId]?.map(
              (request) => request.appearance
            ) ?? [],
        };
      },

      getBundle:
        (state) =>
        (id: string): CriminalDocumentBundle | undefined => {
          return state.bundles.find((bundle) => bundle.id === id);
        },
    },

    actions: {
      syncRequest(items: CriminalDocumentAppearanceRequest[]): void {
        this.request = {
          appearances: items.map((item) => item.appearance),
        };
      },

      setPdfItems(
        items: CriminalDocumentAppearanceRequest[],
        sessionId = uuidv4()
      ): string {
        this.sessions[sessionId] = [...items];
        this.activeSessionId = sessionId;

        this.syncRequest(items);

        return sessionId;
      },

      setAppearanceRequests(
        items: CriminalDocumentAppearanceRequest[],
        sessionId = uuidv4()
      ): string {
        return this.setPdfItems(items, sessionId);
      },

      addAppearanceRequest(
        item: CriminalDocumentAppearanceRequest,
        sessionId?: string
      ): string {
        const resolvedSessionId = sessionId ?? this.activeSessionId ?? uuidv4();

        if (!this.sessions[resolvedSessionId]) {
          this.sessions[resolvedSessionId] = [];
        }

        this.sessions[resolvedSessionId].push(item);
        this.activeSessionId = resolvedSessionId;

        this.syncRequest(this.sessions[resolvedSessionId]);

        return resolvedSessionId;
      },

      addBundle(id: string): void {
        this.bundles.push({
          id,
          binders: [],
          groupKeyOne: '',
          groupKeyTwo: '',
          documentName: '',
          physicalFileId: '',
          requests: { appearances: [] },
        });
      },

      addBinder(binder: Binder, bundleId: string): void {
        const bundle = this.bundles.find((b) => b.id === bundleId);

        if (bundle) {
          bundle.binders.push(binder);
        }
      },

      clearPdfItems(sessionId?: string): void {
        const resolvedSessionId = sessionId ?? this.activeSessionId;

        if (!resolvedSessionId) {
          return;
        }

        delete this.sessions[resolvedSessionId];

        if (this.activeSessionId === resolvedSessionId) {
          this.activeSessionId = null;
          this.request = { appearances: [] };
        }
      },

      clearBundles(sessionId?: string): void {
        this.clearPdfItems(sessionId);
        this.bundles.length = 0;
      },

      clearAllSessions(): void {
        this.sessions = {};
        this.activeSessionId = null;
        this.request = { appearances: [] };
        this.bundles.length = 0;
      },
    },
  }
);

type CriminalDocumentBundleStoreState = {
  activeSessionId: string | null;
  sessions: Record<string, CriminalDocumentAppearanceRequest[]>;
  bundles: CriminalDocumentBundle[];
  request: CriminalDocumentBundleRequest;
};

export type CriminalDocumentBundle = {
  id: string;
  groupKeyOne: string;
  groupKeyTwo: string;
  physicalFileId: string;
  documentName: string;
  requests: CriminalDocumentBundleRequest;
  binders: Binder[];
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
