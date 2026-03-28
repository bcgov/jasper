import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('main bootstrap', () => {
  const registerPlugins = vi.fn();
  const registerRouter = vi.fn();
  const registerPinia = vi.fn();
  const callRefreshLinkClickTracking = vi.fn();
  const getSettings = vi.fn();
  const setIsInitializing = vi.fn();

  const use = vi.fn();
  const runWithContext = vi.fn((cb: () => unknown) => cb());
  const mount = vi.fn();

  const observe = vi.fn();
  const pushStateSpy = vi.spyOn(history, 'pushState');

  const router = { name: 'router' };

  const loadMain = async (options?: {
    pathname?: string;
    rejectSettings?: boolean;
  }) => {
    vi.resetModules();

    registerPlugins.mockClear();
    registerRouter.mockClear();
    registerPinia.mockClear();
    callRefreshLinkClickTracking.mockClear();
    getSettings.mockClear();
    setIsInitializing.mockClear();
    use.mockClear();
    runWithContext.mockClear();
    mount.mockClear();
    observe.mockClear();
    pushStateSpy.mockClear();

    const pathname = options?.pathname ?? '/';
    history.pushState({}, '', pathname);

    class MutationObserverMock {
      constructor(private readonly _callback: MutationCallback) {}

      observe = observe;
    }

    vi.stubGlobal('MutationObserver', MutationObserverMock);

    if (options?.rejectSettings) {
      getSettings.mockRejectedValueOnce(new Error('bootstrap failed'));
    } else {
      getSettings.mockResolvedValueOnce(true);
    }

    vi.doMock('vue', async () => {
      const actual = await vi.importActual<typeof import('vue')>('vue');
      return {
        ...actual,
        createApp: vi.fn(() => ({
          use,
          runWithContext,
          mount,
        })),
      };
    });

    vi.doMock('@/App.vue', () => ({
      default: { name: 'App' },
    }));

    vi.doMock('@/plugins', () => ({
      registerPlugins,
    }));

    vi.doMock('@/utils/snowplowUtils', () => ({
      callRefreshLinkClickTracking,
    }));

    vi.doMock('@/utils/utils', () => ({
      SessionManager: {
        getSettings,
      },
    }));

    vi.doMock('@/router/index', () => ({
      default: router,
    }));

    vi.doMock('@/services', () => ({
      registerRouter,
    }));

    vi.doMock('@/stores', () => ({
      registerPinia,
      useCommonStore: () => ({
        setIsInitializing,
      }),
    }));

    await import('@/main');
  };

  beforeEach(() => {
    vi.stubEnv('BASE_URL', '/jasper/');
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    vi.clearAllMocks();
  });

  it('bootstraps app, initializes session settings, and clears loading state', async () => {
    await loadMain({ pathname: '/' });

    expect(registerPinia).toHaveBeenCalledTimes(1);
    expect(registerRouter).toHaveBeenCalledTimes(1);
    expect(registerPlugins).toHaveBeenCalledTimes(1);
    expect(use).toHaveBeenCalledWith(router);
    expect(runWithContext).toHaveBeenCalledTimes(1);
    expect(getSettings).toHaveBeenCalledTimes(1);
    expect(mount).toHaveBeenCalledWith('#app');

    expect(setIsInitializing).toHaveBeenNthCalledWith(1, true);
    expect(setIsInitializing).toHaveBeenLastCalledWith(false);

    expect(callRefreshLinkClickTracking).toHaveBeenCalledTimes(1);
    expect(observe).toHaveBeenCalledWith(document.body, {
      childList: true,
      subtree: true,
    });
    expect(pushStateSpy).toHaveBeenCalled();
  });

  it('handles bootstrap failure and still clears loading state', async () => {
    const consoleErrorSpy = vi
      .spyOn(console, 'error')
      .mockImplementation(() => undefined);

    await loadMain({ pathname: '/dashboard', rejectSettings: true });

    expect(getSettings).toHaveBeenCalledTimes(1);
    expect(setIsInitializing).toHaveBeenCalledWith(true);
    expect(setIsInitializing).toHaveBeenCalledWith(false);
    expect(callRefreshLinkClickTracking).not.toHaveBeenCalled();
    expect(consoleErrorSpy).toHaveBeenCalledWith(
      'Failed to bootstrap application.',
      expect.any(Error)
    );
    expect(pushStateSpy).not.toHaveBeenCalledWith(
      { page: 'home' },
      '',
      expect.any(String)
    );

    consoleErrorSpy.mockRestore();
  });
});
