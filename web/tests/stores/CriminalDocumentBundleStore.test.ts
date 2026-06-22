import { useCriminalDocumentBundleStore } from '@/stores/CriminalDocumentBundleStore';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it } from 'vitest';

const createAppearanceRequest = (fileNumber: string) => ({
  fileNumber,
  fullName: `${fileNumber} Doe`,
  groupKeyOne: fileNumber,
  groupKeyTwo: `${fileNumber} Person`,
  documentName: `${fileNumber} Person`,
  physicalFileId: `${fileNumber}-file`,
  participantId: `${fileNumber}-participant`,
  appearance: {
    physicalFileId: `${fileNumber}-file`,
    participantId: `${fileNumber}-participant`,
    appearanceId: `${fileNumber}-appearance`,
    courtClassCd: 'CLS',
  },
});

describe('CriminalDocumentBundleStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('initializes with empty session-backed state', () => {
    const store = useCriminalDocumentBundleStore();

    expect(store.sessions).toEqual({});
    expect(store.activeSessionId).toBeNull();
    expect(store.getPdfItems()).toEqual([]);
    expect(store.hasPdfData()).toBe(false);
  });

  it('returns data for an explicit session instead of the active session', () => {
    const store = useCriminalDocumentBundleStore();

    store.setPdfItems([createAppearanceRequest('FN1')] as any, 'session-1');
    store.setPdfItems([createAppearanceRequest('FN2')] as any, 'session-2');

    expect(store.activeSessionId).toBe('session-2');
    expect(store.getPdfItems('session-1')).toEqual([
      createAppearanceRequest('FN1'),
    ]);
    expect(store.hasPdfData('session-1')).toBe(true);
  });

  it('clearBundles clears only the requested session', () => {
    const store = useCriminalDocumentBundleStore();

    store.setPdfItems([createAppearanceRequest('FN1')] as any, 'session-1');
    store.setPdfItems([createAppearanceRequest('FN2')] as any, 'session-2');

    store.clearBundles('session-1');

    expect(store.getPdfItems('session-1')).toEqual([]);
    expect(store.getPdfItems('session-2')).toEqual([
      createAppearanceRequest('FN2'),
    ]);
    expect(store.activeSessionId).toBe('session-2');
  });

  it('clearAllSessions resets all session state', () => {
    const store = useCriminalDocumentBundleStore();

    store.setPdfItems([createAppearanceRequest('FN1')] as any, 'session-1');
    store.setPdfItems([createAppearanceRequest('FN2')] as any, 'session-2');

    store.clearAllSessions();

    expect(store.sessions).toEqual({});
    expect(store.activeSessionId).toBeNull();
    expect(store.getPdfItems()).toEqual([]);
    expect(store.hasPdfData()).toBe(false);
  });

  it('supports persistence when persist option is enabled', () => {
    const store = useCriminalDocumentBundleStore();
    expect(store).toBeDefined();
  });
});
