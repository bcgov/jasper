import { defineStore } from 'pinia';
import { UUIDTypes } from 'uuid';
import { Binder } from '@/types/Binder';
  import {
    CourtListAppearanceDocumentRequest,
    CourtListDocumentBundleRequest,
  } from '@/types/courtlist/jsonTypes';
  

export const useBundleStore = defineStore('BundleStore', {
  persist: true,
  state: () => ({
    bundles: [] as Bundle[],
    request: {} as CourtListDocumentBundleRequest,
    appearanceRequests: [] as appearanceRequest[],
  }),
  getters: {
    getBundle: (state): ((id: UUIDTypes) => Bundle | undefined) => {
      return (id: UUIDTypes) => {
        const bundle = state.bundles.find(b => b.id === id);
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
        requests: {} as CourtListDocumentBundleRequest,
      });
    },
    addBinder(binder: Binder, bundleId: UUIDTypes): void {
      const bundle = this.bundles.find(b => b.id === bundleId);
      if (bundle) {
        bundle.binders.push(binder);
      }
    },
    clearBundle(): void {
    },
  },
});

export type Bundle = {
  id: UUIDTypes;
  groupKeyOne: string;
  groupKeyTwo: string;
  documentName: string;
  requests: CourtListDocumentBundleRequest;
  binders: Binder[];
};

export type appearanceRequest = {
  appearance: CourtListAppearanceDocumentRequest;
  fileNumber: string;
  fullName: string;
};
