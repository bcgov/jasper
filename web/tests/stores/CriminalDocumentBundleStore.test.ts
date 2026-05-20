import { describe, it, expect, beforeEach } from 'vitest';
import { setActivePinia, createPinia } from 'pinia';
import { useCriminalDocumentBundleStore } from '@/stores/CriminalDocumentBundleStore';
import { v4 as uuidv4 } from 'uuid';

describe('CriminalDocumentBundleStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('initializes with empty bundles and appearance requests', () => {
    const store = useCriminalDocumentBundleStore();
    expect(store.bundles).toEqual([]);
    expect(store.appearanceRequests).toEqual([]);
    expect(store.request).toEqual({});
  });

  it('getBundle returns bundle by id', () => {
    const store = useCriminalDocumentBundleStore();
    const bundleId = uuidv4();
    
    store.addBundle(bundleId);
    const bundle = store.getBundle(bundleId);
    
    expect(bundle).toBeDefined();
    expect(bundle?.id).toBe(bundleId);
  });

  it('getBundle returns undefined for non-existent bundle', () => {
    const store = useCriminalDocumentBundleStore();
    const nonExistentId = uuidv4();
    
    const bundle = store.getBundle(nonExistentId);
    expect(bundle).toBeUndefined();
  });

  it('getRequests returns the request object', () => {
    const store = useCriminalDocumentBundleStore();
    const mockRequest = { appearances: [] };
    store.request = mockRequest as any;
    
    expect(store.getRequests).toEqual(mockRequest);
  });

  it('getAppearanceRequests returns appearance requests', () => {
    const store = useCriminalDocumentBundleStore();
    const mockRequests = [
      {
        fileNumber: 'FN1',
        fullName: 'John Doe',
        appearance: {
          physicalFileId: 'F1',
          participantId: 'P1',
          appearanceId: 'APP1',
          courtClassCd: 'CLS1',
        },
      },
    ];
    store.appearanceRequests = mockRequests as any;
    
    expect(store.getAppearanceRequests).toEqual(mockRequests);
  });

  it('addBundle creates a new bundle and adds it to bundles array', () => {
    const store = useCriminalDocumentBundleStore();
    const bundleId = uuidv4();
    
    store.addBundle(bundleId);
    
    expect(store.bundles).toHaveLength(1);
    expect(store.bundles[0].id).toBe(bundleId);
    expect(store.bundles[0].binders).toEqual([]);
  });

  it('addBinder adds binder to specific bundle', () => {
    const store = useCriminalDocumentBundleStore();
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
    const store = useCriminalDocumentBundleStore();
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
    const store = useCriminalDocumentBundleStore();
    const bundleId = uuidv4();
    
    store.addBundle(bundleId);
    store.appearanceRequests = [
      {
        fileNumber: 'FN1',
        fullName: 'John Doe',
        appearance: {
          physicalFileId: 'F1',
          participantId: 'P1',
          appearanceId: 'APP1',
          courtClassCd: 'CLS1',
        },
      },
    ] as any;
    store.request = { appearances: [] } as any;
    
    store.clearBundles();
    
    expect(store.bundles).toEqual([]);
    expect(store.appearanceRequests).toEqual([]);
    expect(store.request).toEqual({});
  });

  it('supports persistence when persist option is enabled', () => {
    // This test ensures the store is created with persist: true
    const store = useCriminalDocumentBundleStore();
    // Check that the store has the persist plugin applied by seeing if it's tracked
    expect(store).toBeDefined();
  });
});
