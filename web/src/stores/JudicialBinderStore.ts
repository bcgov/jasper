import { defineStore } from 'pinia';
import { UUIDTypes } from 'uuid';
import { Binder } from '@/types/Binder';
import { BinderDocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { BinderDocumentRequest } from '@/types/BinderDocumentRequest';

export const useJudicialBinderStore = defineStore('JudicialBinderStore', {
  persist: true,
  state: () => ({
    bundles: [] as JudicialBinderBundle[],
    request: {} as BinderDocumentBundleRequest,
  }),
  getters: {
    getBundle: (
      state
    ): ((id: UUIDTypes) => JudicialBinderBundle | undefined) => {
      return (id: UUIDTypes) => {
        const bundle = state.bundles.find((b) => b.id === id);
        return bundle;
      };
    },
    getRequests: (state) => state.request,
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
        requests: {} as BinderDocumentBundleRequest,
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
      this.request = {} as BinderDocumentBundleRequest;
    },
  },
});

export type JudicialBinderBundle = {
  id: UUIDTypes;
  groupKeyOne: string;
  groupKeyTwo: string;
  physicalFileId: string;
  documentName: string;
  requests: BinderDocumentBundleRequest;
  binders: Binder[];
};

export type JudicialBinderDocumentRequest = {
  binder: BinderDocumentRequest;
  fileNumber: string;
};
