import { defineStore } from 'pinia';
import { UUIDTypes } from 'uuid';
import { Binder } from '@/types/Binder';
import { DocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { AppearanceDocumentRequest } from '@/types/AppearanceDocumentRequest';

export const useBundleStore = defineStore('BundleStore', {
  persist: true,
  state: () => ({
    bundles: [] as Bundle[],
    request: {} as DocumentBundleRequest,
    appearanceRequests: [] as appearanceRequest[],
  }),
  getters: {
    getBundle: (state): ((id: UUIDTypes) => Bundle | undefined) => {
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
        requests: {} as DocumentBundleRequest,
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
      this.request = {} as DocumentBundleRequest;
    },
  },
});

export type Bundle = {
  id: UUIDTypes;
  groupKeyOne: string;
  groupKeyTwo: string;
  documentName: string;
  requests: DocumentBundleRequest;
  binders: Binder[];
};

export type appearanceRequest = {
  appearance: AppearanceDocumentRequest;
  fileNumber: string;
  fullName: string;
};
