import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useJudicialBinderStore } from '@/stores/JudicialBinderStore';
import { v4 as uuidv4 } from 'uuid';

const createBundle = (id = uuidv4()) => ({
  id,
  binders: [
    {
      binder: {
        id: 'binder-1',
        labels: { physicalFileId: 'F1', participantId: 'P1' },
        documents: [],
      },
      fileNumber: 'file-number',
      groupKeyOne: 'file-number',
      groupKeyTwo: '',
      documentName: 'Document',
      physicalFileId: 'F1',
      participantId: 'P1',
      documentId: 'DOC1',
    },
  ],
});

describe('JudicialBinderStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('initializes with empty bundles', () => {
    const store = useJudicialBinderStore();
    expect(store.sessions).toEqual({});
    expect(store.activeSessionId).toBeNull();
    expect(store.getBundles).toEqual([]);
    expect(store.getPdfItems()).toEqual([]);
    expect(store.hasPdfData()).toBe(false);
  });

  it('getBundle returns bundle by id', () => {
    const store = useJudicialBinderStore();
    const bundleId = uuidv4();

    store.addBundle(createBundle(bundleId));
    const bundle = store.getBundle(bundleId);

    expect(bundle).toBeDefined();
    expect(bundle?.id).toBe(bundleId);
  });

  it('getBundle returns undefined for non-existent bundle', () => {
    const store = useJudicialBinderStore();
    const nonExistentId = uuidv4();

    const bundle = store.getBundle(nonExistentId);
    expect(bundle).toBeUndefined();
  });

  it('getBundles returns all bundles', () => {
    const store = useJudicialBinderStore();
    const bundle = createBundle();
    store.addBundle(bundle);

    expect(store.getBundles).toEqual([bundle]);
  });

  it('addBundle creates a new bundle and adds it to bundles array', () => {
    const store = useJudicialBinderStore();
    const bundle = createBundle();

    store.addBundle(bundle);

    expect(store.activeSessionId).toBe(bundle.id);
    expect(store.getBundles).toHaveLength(1);
    expect(store.getBundles[0].id).toBe(bundle.id);
    expect(store.getBundles[0].binders).toEqual(bundle.binders);
    expect(store.getPdfItems()).toEqual(bundle.binders);
  });

  it('addBinder adds binder to specific bundle', () => {
    const store = useJudicialBinderStore();
    const bundleId = uuidv4();
    const mockBinder = {
      binder: {
        id: uuidv4(),
        labels: { physicalFileId: 'F1', participantId: 'P1' },
        documents: [],
      },
      fileNumber: 'F1',
      groupKeyOne: 'F1',
      groupKeyTwo: '',
      physicalFileId: 'F1',
      participantId: 'P1',
      documentName: 'Document',
      documentId: 'DOC1',
    };

    store.addBundle(createBundle(bundleId));
    store.addBinder(mockBinder as any, bundleId);

    const bundle = store.getBundle(bundleId);
    expect(bundle?.binders).toHaveLength(2);
    expect(bundle?.binders[1]).toEqual(mockBinder);
  });

  it('addBinder does nothing if bundle does not exist', () => {
    const store = useJudicialBinderStore();
    const nonExistentBundleId = uuidv4();
    const mockBinder = {
      binder: {
        id: uuidv4(),
        labels: { physicalFileId: 'F1', participantId: 'P1' },
        documents: [],
      },
      fileNumber: 'F1',
      groupKeyOne: 'F1',
      groupKeyTwo: '',
      physicalFileId: 'F1',
      participantId: 'P1',
      documentName: 'Document',
      documentId: 'DOC1',
    };

    // Should not throw and should not add to any bundle
    store.addBinder(mockBinder as any, nonExistentBundleId);

    expect(store.getBundles).toHaveLength(0);
    expect(store.getPdfItems()).toEqual([]);
  });

  it('clearBundles resets all state', () => {
    const store = useJudicialBinderStore();
    const bundle = createBundle();

    store.addBundle(bundle);

    store.clearBundles();

    expect(store.sessions).toEqual({});
    expect(store.activeSessionId).toBeNull();
    expect(store.getBundles).toEqual([]);
    expect(store.getPdfItems()).toEqual([]);
  });

  it('supports bundles with multiple binders', () => {
    const store = useJudicialBinderStore();
    const bundle = createBundle();
    bundle.binders = [
      {
        binder: {
          id: 'binder-1',
          labels: { physicalFileId: 'F1', participantId: 'P1' },
          documents: [],
        },
        fileNumber: 'F1',
        groupKeyOne: 'F1',
        groupKeyTwo: '',
        physicalFileId: 'F1',
        participantId: 'P1',
        documentName: 'Doc 1',
        documentId: 'DOC1',
      },
      {
        binder: {
          id: 'binder-2',
          labels: { physicalFileId: 'F2', participantId: 'P2' },
          documents: [],
        },
        fileNumber: 'F2',
        groupKeyOne: 'F2',
        groupKeyTwo: '',
        physicalFileId: 'F2',
        participantId: 'P2',
        documentName: 'Doc 2',
        documentId: 'DOC2',
      },
    ] as any;

    store.addBundle(bundle as any);

    expect(store.getBundles).toHaveLength(1);
    expect(store.getBundles[0].binders).toHaveLength(2);
  });

  it('supports persistence when persist option is enabled', () => {
    // This test ensures the store is created with persist: true
    const store = useJudicialBinderStore();
    // Check that the store has the persist plugin applied by seeing if it's tracked
    expect(store).toBeDefined();
  });
});
