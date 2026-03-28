import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

interface TestUserInfo {
  userId?: string;
  judgeId?: number;
  judgeHomeLocationId?: number;
  userTitle?: string;
  roles?: string[];
}

interface TestAppInfo {
  version?: string;
}

interface SessionManagerLoadOptions {
  userInfo?: TestUserInfo | null;
  appInfo?: TestAppInfo | null;
  myUserInfo?: Partial<TestUserInfo> | null;
  currentUserInfo?: Partial<TestUserInfo> | null;
}

interface MockCommonStore {
  userInfo: Partial<TestUserInfo> | null;
  appInfo: TestAppInfo | null;
  setLoggedInUserInfo: ReturnType<typeof vi.fn>;
  setUserInfo: ReturnType<typeof vi.fn>;
}

describe('initializeSessionSettings', () => {
  beforeEach(() => {
    vi.resetModules();
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  const loadSessionInitialization = async (
    options?: SessionManagerLoadOptions
  ) => {
    const mockStore: MockCommonStore = {
      userInfo: options?.currentUserInfo ?? null,
      appInfo: null,
      setLoggedInUserInfo: vi.fn(),
      setUserInfo: vi.fn(),
    };

    const authService = {
      getUserInfo: vi.fn().mockResolvedValue(options?.userInfo),
    };
    const appService = {
      getApplicationInfo: vi.fn().mockResolvedValue(options?.appInfo ?? null),
    };
    const userService = {
      getMyUser: vi.fn().mockResolvedValue(options?.myUserInfo ?? null),
    };

    vi.doMock('@/stores', () => ({
      useCommonStore: () => mockStore,
    }));

    vi.doMock('vue', async () => {
      const actual = await vi.importActual<typeof import('vue')>('vue');
      return {
        ...actual,
        inject: (key: string) => {
          if (key === 'authService') return authService;
          if (key === 'applicationService') return appService;
          if (key === 'userService') return userService;
          return undefined;
        },
      };
    });

    const module = await import('@/utils/utils');

    return {
      initializeSessionSettings: module.initializeSessionSettings,
      mockStore,
      authService,
      appService,
      userService,
    };
  };

  it('returns false and clears user info when auth user info is unavailable', async () => {
    const errorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined);

    const { initializeSessionSettings, mockStore, appService, userService } =
      await loadSessionInitialization({
        userInfo: null,
      });

    const result = await initializeSessionSettings();

    expect(result).toBe(false);
    expect(mockStore.setLoggedInUserInfo).toHaveBeenCalledWith(null);
    expect(mockStore.setUserInfo).toHaveBeenCalledWith(null);
    expect(appService.getApplicationInfo).not.toHaveBeenCalled();
    expect(userService.getMyUser).not.toHaveBeenCalled();

    errorSpy.mockRestore();
  });

  it('merges user info and preserves judge override from current store user', async () => {
    const { initializeSessionSettings, mockStore } =
      await loadSessionInitialization({
        userInfo: {
          userId: 'u-1',
          judgeId: 10,
          judgeHomeLocationId: 100,
          userTitle: 'Judge A',
        },
        myUserInfo: {
          roles: ['Role1'],
        },
        appInfo: {
          version: '1.0.0',
        },
        currentUserInfo: {
          judgeId: 99,
          judgeHomeLocationId: 999,
        },
      });

    const result = await initializeSessionSettings();

    expect(result).toBe(true);
    expect(mockStore.setLoggedInUserInfo).toHaveBeenCalledWith(
      expect.objectContaining({ userId: 'u-1' })
    );
    expect(mockStore.setUserInfo).toHaveBeenCalledWith(
      expect.objectContaining({
        userId: 'u-1',
        judgeId: 99,
        judgeHomeLocationId: 999,
        roles: ['Role1'],
      })
    );
    expect(mockStore.appInfo).toEqual({ version: '1.0.0' });
  });

  it('initializes each time it is called', async () => {
    const { initializeSessionSettings, authService } =
      await loadSessionInitialization({
        userInfo: {
          userId: 'u-1',
          judgeId: 10,
        },
        myUserInfo: {},
        appInfo: {},
      });

    await initializeSessionSettings();
    await initializeSessionSettings();

    expect(authService.getUserInfo).toHaveBeenCalledTimes(2);
  });
});
