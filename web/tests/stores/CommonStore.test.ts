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

  it('updates initialization state with setIsInitializing', () => {
    const store = useCommonStore();

    expect(store.isInitializing).toBe(true);
    store.setIsInitializing(false);

    expect(store.isInitializing).toBe(false);
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
