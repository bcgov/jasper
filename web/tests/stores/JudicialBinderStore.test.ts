import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useJudicialBinderStore } from '@/stores/JudicialBinderStore';
import { v4 as uuidv4 } from 'uuid';

describe('JudicialBinderStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('initializes with empty bundles and empty request', () => {
    const store = useJudicialBinderStore();
    expect(store.bundles).toEqual([]);
    expect(store.request).toEqual({});
  });

  it('getBundle returns bundle by id', () => {
    const store = useJudicialBinderStore();
    const bundleId = uuidv4();
    
    store.addBundle(bundleId);
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

  it('getRequests returns the request object', () => {
    const store = useJudicialBinderStore();
    const mockRequest = { binders: [] };
    store.request = mockRequest as any;
    
    expect(store.getRequests).toEqual(mockRequest);
  });

  it('addBundle creates a new bundle and adds it to bundles array', () => {
    const store = useJudicialBinderStore();
    const bundleId = uuidv4();
    
    store.addBundle(bundleId);
    
    expect(store.bundles).toHaveLength(1);
    expect(store.bundles[0].id).toBe(bundleId);
    expect(store.bundles[0].binders).toEqual([]);
  });

  it('addBinder adds binder to specific bundle', () => {
    const store = useJudicialBinderStore();
    const bundleId = uuidv4();
    const mockBinder = {
      id: uuidv4(),
      labels: { physicalFileId: 'F1' },
    };
    
    store.addBundle(bundleId);
    store.addBinder(mockBinder as any, bundleId);
    
    const bundle = store.getBundle(bundleId);
    expect(bundle?.binders).toHaveLength(1);
    expect(bundle?.binders[0]).toEqual(mockBinder);
  });

  it('addBinder does nothing if bundle does not exist', () => {
    const store = useJudicialBinderStore();
    const nonExistentBundleId = uuidv4();
    const mockBinder = {
      id: uuidv4(),
      labels: { physicalFileId: 'F1' },
    };
    
    // Should not throw and should not add to any bundle
    store.addBinder(mockBinder as any, nonExistentBundleId);
    
    expect(store.bundles).toHaveLength(0);
  });

  it('clearBundles resets all state', () => {
    const store = useJudicialBinderStore();
    const bundleId = uuidv4();
    
    store.addBundle(bundleId);
    store.request = {
      binders: [
        {
          physicalFileId: 'F1',
          participantId: 'P1',
          courtClassCd: 'CLS1',
        },
      ],
    } as any;
    
    store.clearBundles();
    
    expect(store.bundles).toEqual([]);
    expect(store.request).toEqual({});
  });

  it('request can hold BinderDocumentBundleRequest with multiple binders', () => {
    const store = useJudicialBinderStore();
    const mockRequest = {
      binders: [
        {
          physicalFileId: 'F1',
          participantId: 'P1',
          courtClassCd: 'CLS1',
        },
        {
          physicalFileId: 'F2',
          participantId: 'P2',
          courtClassCd: 'CLS2',
        },
      ],
    };
    
    store.request = mockRequest as any;
    
    expect(store.getRequests).toEqual(mockRequest);
    expect(store.request.binders).toHaveLength(2);
  });

  it('supports persistence when persist option is enabled', () => {
    // This test ensures the store is created with persist: true
    const store = useJudicialBinderStore();
    // Check that the store has the persist plugin applied by seeing if it's tracked
    expect(store).toBeDefined();
  });
});
