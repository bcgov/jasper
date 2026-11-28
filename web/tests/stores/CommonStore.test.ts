import { SESSION_STATES } from '@/constants/sessionStates';
import {
  ApplicationConfigurationKey,
  useCommonStore,
} from '@/stores/CommonStore';
import { ApplicationInfo, UserInfo } from '@/types/common';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it } from 'vitest';

describe('CommonStore', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
  });

  it('clears session data while retaining existing preferences', () => {
    const store = useCommonStore();

    expect(store.sessionState).toBe(SESSION_STATES.INITIALIZING);
    store.$patch({
      sessionState: SESSION_STATES.AUTHORIZED,
      loggedInUserInfo: { userId: 'user-1' } as UserInfo,
      userInfo: { userId: 'user-1' } as UserInfo,
      appInfo: { version: '1.0' } as ApplicationInfo,
      displayName: 'Saved name',
    });
    store.clearSession(SESSION_STATES.FAILED);

    expect(store.sessionState).toBe(SESSION_STATES.FAILED);
    expect(store.loggedInUserInfo).toBeNull();
    expect(store.userInfo).toBeNull();
    expect(store.appInfo).toBeNull();
    expect(store.displayName).toBe('Saved name');
  });

  it('prefers userInfo title over loggedInUserInfo title', () => {
    const store = useCommonStore();
    store.loggedInUserInfo = { userTitle: 'Logged In User' } as UserInfo;
    store.userInfo = { userTitle: 'Current User' } as UserInfo;

    expect(store.currentUserTitle).toBe('Current User');
  });

  it('falls back to loggedInUserInfo title when userInfo is empty', () => {
    const store = useCommonStore();
    store.loggedInUserInfo = { userTitle: 'Logged In User' } as UserInfo;
    store.userInfo = null;

    expect(store.currentUserTitle).toBe('Logged In User');
  });

  it('returns release notes url from application configuration', () => {
    const store = useCommonStore();
    store.appInfo = {
      version: '1.2.3',
      nutrientFeLicenseKey: 'k',
      environment: 'test',
      configuration: [
        {
          id: 'config-1',
          key: ApplicationConfigurationKey.ReleaseNotesUrl,
          values: ['https://example.com/release-notes'],
        },
      ],
    } as ApplicationInfo;

    expect(store.releaseNotesUrl).toBe('https://example.com/release-notes');
    expect(store.currentAppVersion).toBe('1.2.3');
  });

  it('returns false for hasUnviewedReleaseNotes when url or version is missing', () => {
    const store = useCommonStore();
    store.appInfo = {
      version: '',
      nutrientFeLicenseKey: 'k',
      environment: 'test',
      configuration: [],
    } as ApplicationInfo;
    store.userInfo = {
      releaseNotes: { lastViewedVersion: '1.0.0' },
    } as UserInfo;

    expect(store.hasUnviewedReleaseNotes).toBe(false);
  });

  it('returns true for hasUnviewedReleaseNotes when viewed version differs', () => {
    const store = useCommonStore();
    store.appInfo = {
      version: '2.0.0',
      nutrientFeLicenseKey: 'k',
      environment: 'test',
      configuration: [
        {
          id: 'config-1',
          key: ApplicationConfigurationKey.ReleaseNotesUrl,
          values: ['https://example.com/release-notes'],
        },
      ],
    } as ApplicationInfo;
    store.userInfo = {
      releaseNotes: { lastViewedVersion: '1.9.0' },
    } as UserInfo;

    expect(store.hasUnviewedReleaseNotes).toBe(true);
  });

  it('returns false for hasUnviewedReleaseNotes when viewed version matches', () => {
    const store = useCommonStore();
    store.appInfo = {
      version: '2.0.0',
      nutrientFeLicenseKey: 'k',
      environment: 'test',
      configuration: [
        {
          id: 'config-1',
          key: ApplicationConfigurationKey.ReleaseNotesUrl,
          values: ['https://example.com/release-notes'],
        },
      ],
    } as ApplicationInfo;
    store.userInfo = {
      releaseNotes: { lastViewedVersion: '2.0.0' },
    } as UserInfo;

    expect(store.hasUnviewedReleaseNotes).toBe(false);
  });

  it('returns configuration values and first value helpers', () => {
    const store = useCommonStore();
    store.appInfo = {
      version: '2.0.0',
      nutrientFeLicenseKey: 'k',
      environment: 'test',
      configuration: [
        {
          id: 'config-1',
          key: ApplicationConfigurationKey.ReleaseNotesUrl,
          values: [
            'https://example.com/release-notes',
            'https://fallback.example',
          ],
        },
      ],
    } as ApplicationInfo;

    expect(
      store.getConfigurationValues(ApplicationConfigurationKey.ReleaseNotesUrl)
    ).toEqual([
      'https://example.com/release-notes',
      'https://fallback.example',
    ]);
    expect(
      store.getConfigurationValue(ApplicationConfigurationKey.ReleaseNotesUrl)
    ).toBe('https://example.com/release-notes');
  });
});
