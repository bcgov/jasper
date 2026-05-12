import { defineStore } from 'pinia';
import { UUIDTypes } from 'uuid';
import { Binder } from '@/types/Binder';
import { KeyDocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { AppearanceDocumentRequest } from '@/types/AppearanceDocumentRequest';

export const useCriminalDocumentBundleStore = defineStore('CriminalDocumentBundleStore', {
  persist: true,
  state: () => ({
    bundles: [] as KeyDocumentBundle[],
    request: {} as KeyDocumentBundleRequest,
    appearanceRequests: [] as KeyDocumentAppearanceRequest[],
  }),
  getters: {
    getBundle: (state): ((id: UUIDTypes) => KeyDocumentBundle | undefined) => {
      return (id: UUIDTypes) => {
        const bundle = state.bundles.find((b) => b.id === id);
        return bundle;
      };
    },
    getRequests: (state) => state.request,
    getAppearanceRequests: (state) => state.appearanceRequests,
  },
  actions: {
    addBundle(id: UUIDTypes): void {
      this.bundles.push({
        id: id,
        binders: [] as Binder[],
        groupKeyOne: '',
        groupKeyTwo: '',
        documentName: '',
        physicalFileId: '',
        requests: {} as KeyDocumentBundleRequest,
      });
    },
    addBinder(binder: Binder, bundleId: UUIDTypes): void {
      const bundle = this.bundles.find((b) => b.id === bundleId);
      if (bundle) {
        bundle.binders.push(binder);
      }
    },
    clearBundles(): void {
      this.bundles.length = 0;
      this.appearanceRequests = [];
      this.request = {} as KeyDocumentBundleRequest;
    },
  },
});

export type KeyDocumentBundle = {
  id: UUIDTypes;
  groupKeyOne: string;
  groupKeyTwo: string;
  physicalFileId: string;
  documentName: string;
  requests: KeyDocumentBundleRequest;
  binders: Binder[];
};

export type KeyDocumentAppearanceRequest = {
  appearance: AppearanceDocumentRequest;
  fileNumber: string;
  fullName?: string;
};
