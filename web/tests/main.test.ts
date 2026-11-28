import { SESSION_STATES, type SessionState } from '@/constants/sessionStates';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

describe('main bootstrap', () => {
  const registerPlugins = vi.fn();
  const registerRouter = vi.fn();
  const registerPinia = vi.fn();
  const callRefreshLinkClickTracking = vi.fn();
  const router = { name: 'router' };
  const createAppRouter = vi.fn(() => router);
  const store = {
    sessionState: SESSION_STATES.AUTHORIZED as SessionState,
    clearSession: vi.fn(),
  };
  const use = vi.fn();
  const mount = vi.fn();
  const observe = vi.fn();

  const loadMain = async (pathname: string) => {
    vi.resetModules();
    history.replaceState({}, '', pathname);
    vi.stubGlobal('MutationObserver', class {
      observe = observe;
    });
    vi.doMock('vue', async () => ({
      ...(await vi.importActual<typeof import('vue')>('vue')),
      createApp: vi.fn(() => ({ use, mount })),
    }));
    vi.doMock('@/App.vue', () => ({ default: { name: 'App' } }));
    vi.doMock('@/plugins', () => ({ registerPlugins }));
    vi.doMock('@/utils/snowplowUtils', () => ({ callRefreshLinkClickTracking }));
    vi.doMock('@/router/index', () => ({ createAppRouter }));
    vi.doMock('@/services', () => ({ registerRouter }));
    vi.doMock('@/stores', () => ({
      registerPinia,
      useCommonStore: () => store,
    }));

    await import('@/main');
  };

  beforeEach(() => {
    vi.stubEnv('BASE_URL', '/apps/jasper/');
    store.sessionState = SESSION_STATES.AUTHORIZED;
    createAppRouter.mockImplementation(() => router);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.unstubAllEnvs();
    vi.clearAllMocks();
  });

  it('sets up Pinia, resets session state and provides services before initial navigation', async () => {
    createAppRouter.mockImplementation(() => {
      expect(store.sessionState).toBe(SESSION_STATES.INITIALIZING);
      expect(registerPinia).toHaveBeenCalled();
      expect(registerRouter).toHaveBeenCalled();
      return router;
    });
    await loadMain('/?fromA2A=true#detail');

    expect(registerPlugins).toHaveBeenCalledOnce();
    expect(use).toHaveBeenCalledWith(router);
    expect(mount).toHaveBeenCalledWith('#app');
    expect(location.pathname + location.search + location.hash).toBe('/apps/jasper/?fromA2A=true#detail');
    expect(callRefreshLinkClickTracking).toHaveBeenCalledOnce();
    expect(observe).toHaveBeenCalledWith(document.body, { childList: true, subtree: true });
  });

  it.each([
    ['pcss_authorization_failed', '/'],
    ['authentication_failed', '/apps/jasper/'],
  ])('preserves %s on arrival and refresh, and skips initial router navigation', async (code, path) => {
    const url = path + '?authError=' + code;
    await loadMain(url);
    await loadMain(url);

    expect(store.clearSession).toHaveBeenCalledTimes(2);
    expect(store.clearSession).toHaveBeenCalledWith(SESSION_STATES.FAILED);
    expect(createAppRouter).not.toHaveBeenCalled();
    expect(use).not.toHaveBeenCalled();
    expect(mount).toHaveBeenCalledTimes(2);
    expect(location.pathname + location.search).toBe(url);
  });

  it('clears session state when bootstrap fails', async () => {
    const consoleError = vi.spyOn(console, 'error').mockImplementation(() => {});
    mount.mockImplementationOnce(() => { throw new Error('bootstrap failed'); });
    await loadMain('/apps/jasper/dashboard');
    expect(store.clearSession).toHaveBeenCalledWith(SESSION_STATES.FAILED);
    expect(callRefreshLinkClickTracking).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalledWith('Failed to bootstrap application.', expect.any(Error));
    consoleError.mockRestore();
  });
});
