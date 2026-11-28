import type { PDFViewerToolbarContext } from '@/components/documents/strategies/PDFViewerTypes';
import { PERMISSIONS } from '@/constants/permissions';
import { SESSION_STATES } from '@/constants/sessionStates';
import type { UserInfo } from '@/types/common';
import { flushPromises, mount } from '@vue/test-utils';
import { AxiosError, type InternalAxiosRequestConfig } from 'axios';
import { createPinia, setActivePinia } from 'pinia';
import piniaPluginPersistedstate from 'pinia-plugin-persistedstate';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { nextTick } from 'vue';

vi.mock('@/components/dashboard/Dashboard.vue', () => ({
  default: { template: '<div>Protected dashboard</div>' },
}));
vi.mock('@/components/civil/CivilCaseDetails.vue', () => ({
  default: { template: '<div>Protected civil file</div>' },
}));
vi.mock('@/components/courtlist/CourtList.vue', () => ({
  default: { template: '<div>Protected court list</div>' },
}));
vi.mock('@/components/shared/ProfileOffCanvas.vue', () => ({
  default: { template: '<div>Protected profile</div>' },
}));
vi.mock('@/components/dashboard/DarsAccessModal.vue', () => ({
  default: { template: '<div />' },
}));

const base = '/apps/jasper/';
const attemptKey = 'jasper.signInAttempt';
const claims = {
  userId: 'user-1',
  email: 'judge@example.com',
  isActive: true,
  roles: ['Judge'],
  judgeId: 12,
  judgeHomeLocationId: 3,
  userTitle: 'Judge Example',
};
const databaseUser = {
  id: claims.userId,
  email: claims.email,
  isActive: true,
  roles: ['Judge'],
  judgeId: 12,
  permissions: [PERMISSIONS.VIEW_ORDERS_DASHBOARD],
};

function rejectRequest(status?: number) {
  return (config: InternalAxiosRequestConfig) => {
    const response = status === undefined
      ? undefined
      : { status, statusText: '', data: '', headers: {}, config };
    throw new AxiosError('Request failed', undefined, config, undefined, response);
  };
}

function deferred<T>() {
  let resolve!: (value: T) => void;
  const promise = new Promise<T>((done) => { resolve = done; });
  return { promise, resolve };
}

describe('JASPER session initialization', () => {
  const cleanup: (() => void)[] = [];
  const login = vi.fn();
  const logout = vi.fn();

  beforeEach(() => {
    vi.stubEnv('BASE_URL', base);
    localStorage.clear();
    sessionStorage.clear();
    vi.spyOn(location, 'replace').mockImplementation((path) => {
      const url = new URL(path, location.origin);
      if (url.pathname === base + 'api/auth/login') {
        login(url.searchParams.get('redirectUri'));
      } else if (url.pathname === base + 'api/auth/logout') {
        logout(url.searchParams.get('redirectUri'));
      }
    });
    vi.spyOn(console, 'error').mockImplementation(() => {});
  });

  afterEach(() => {
    for (const dispose of cleanup.splice(0)) dispose();
    vi.restoreAllMocks();
    vi.unstubAllEnvs();
    vi.unstubAllGlobals();
  });

  async function visit(
    path = 'dashboard',
    overrides: Record<string, (config: InternalAxiosRequestConfig) => unknown> = {}
  ) {
    // Simulate a top-level navigation with fresh module state and the same browser storage.
    for (const dispose of cleanup.splice(0)) dispose();
    vi.resetModules();
    history.replaceState(null, '', base + path);
    const [
      { default: App },
      { createAppRouter },
      { ApplicationService },
      { AuthService },
      { HttpService },
      { default: redirectHandlerService },
      { UserService },
      { useCommonStore },
    ] = await Promise.all([
      import('@/App.vue'),
      import('@/router'),
      import('@/services/ApplicationService'),
      import('@/services/AuthService'),
      import('@/services/HttpService'),
      import('@/services/RedirectHandlerService'),
      import('@/services/UserService'),
      import('@/stores/CommonStore'),
    ]);
    const pinia = createPinia().use(piniaPluginPersistedstate);
    setActivePinia(pinia);
    const http = new HttpService(base);
    const replies: Record<string, (config: InternalAxiosRequestConfig) => unknown> = {
      'api/auth/info': () => claims,
      'api/users/me': () => databaseUser,
      'api/application/info': () => ({ version: '1.0' }),
      ...overrides,
    };
    const adapter = vi.fn(async (config: InternalAxiosRequestConfig) => {
      const data = await replies[config.url!](config);
      return { data, status: 200, statusText: 'OK', headers: {}, config };
    });
    http.client.defaults.adapter = adapter;
    const router = createAppRouter();
    const getJudges = vi.fn().mockResolvedValue([]);
    const getOrders = vi.fn().mockResolvedValue([]);
    const startNotifications = vi.fn().mockResolvedValue(undefined);
    const authFailed = redirectHandlerService.hasAuthenticationFailure();
    const wrapper = mount(App, {
      global: {
        plugins: authFailed
          ? [pinia, { install: () => useCommonStore(pinia).clearSession(SESSION_STATES.FAILED) }]
          : [pinia, router],
        provide: {
          authService: new AuthService(http),
          userService: new UserService(http),
          applicationService: new ApplicationService(http),
          judgeService: { getJudges },
          orderService: { getOrders },
          notificationsService: { start: startNotifications, setHandlerProvider: vi.fn() },
        },
      },
    });
    cleanup.push(() => {
      wrapper.unmount();
      router.options.history.destroy();
    });
    return {
      wrapper, router, http, store: useCommonStore(pinia),
      urls: () => adapter.mock.calls.map(([config]) => config.url),
      getJudges, getOrders, startNotifications,
    };
  }

  async function expectFailure(page: Awaited<ReturnType<typeof visit>>) {
    await vi.waitFor(() => {
      expect(page.wrapper.text()).toContain('Unable to sign in to JASPER');
    });
    expect(page.wrapper.text()).not.toContain('Submit Your Request');
    expect(page.wrapper.text()).not.toContain('Protected profile');
    expect(page.getJudges).not.toHaveBeenCalled();
    expect(page.getOrders).not.toHaveBeenCalled();
    expect(page.startNotifications).not.toHaveBeenCalled();
  }

  it('opens a nested A2A deep link and initializes the authorized shell once', async () => {
    const page = await visit('civil-file/123/documents?fromA2A=true#detail');
    await page.router.isReady();
    await flushPromises();

    expect(page.wrapper.text()).toContain('Protected civil file');
    expect(page.wrapper.text()).toContain('Protected profile');
    expect(page.router.currentRoute.value.fullPath).toBe('/civil-file/123/documents?fromA2A=true#detail');
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me', 'api/application/info']);
    expect(page.getJudges).toHaveBeenCalledTimes(1);
    expect(page.getOrders).toHaveBeenCalledExactlyOnceWith(12);
    expect(page.startNotifications).toHaveBeenCalledTimes(1);
    expect(login).not.toHaveBeenCalled();
  });

  it.each([100, 200])('keeps authenticated judge 100 for order review when the mapped judge is %s', async (judgeId) => {
    const authUser = { ...claims, judgeId: 100 };
    const page = await visit('dashboard?id=123', {
      'api/auth/info': () => authUser,
      'api/users/me': () => ({
        ...databaseUser,
        judgeId,
        hasSignature: true,
        hasInitials: true,
      }),
    });
    await page.router.isReady();

    expect(page.store.loggedInUserInfo).toEqual(authUser);
    expect(page.store.userInfo?.judgeId).toBe(judgeId);

    const { OrderPDFStrategy } = await import('@/components/documents/strategies/OrderPDFStrategy');
    const app = page.wrapper.vm.$.appContext.app;
    app.provide('filesService', {});
    const strategy = app.runWithContext(() => new OrderPDFStrategy());
    try {
      const toolbar = strategy.addCustomToolbarItems({} as PDFViewerToolbarContext);
      expect(toolbar.map(({ id }) => id)).toEqual(
        judgeId === 100
          ? ['open-information', 'open-document-review', 'add-signature', 'add-initials']
          : ['open-information']
      );
    } finally {
      strategy.cleanup();
    }
  });

  it.each([undefined, null, 0])('supports the server default judge when the stored mapping is %s', async (judgeId) => {
    const page = await visit('dashboard', {
      'api/auth/info': () => ({ ...claims, judgeId: 75, judgeHomeLocationId: 8 }),
      'api/users/me': () => ({ ...databaseUser, judgeId }),
    });
    await page.router.isReady();
    await flushPromises();

    expect(page.wrapper.text()).toContain('Protected dashboard');
    expect(page.store.loggedInUserInfo?.judgeId).toBe(75);
    expect(page.store.userInfo).toMatchObject({ judgeId: 75, judgeHomeLocationId: 8 });
    expect(page.getOrders).toHaveBeenCalledExactlyOnceWith(75);
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me', 'api/application/info']);
  });

  it('uses stored authorization and the server home-location fallback without checking PCSS', async () => {
    // A successful stored-authorization sign-in uses the normal responses.
    const page = await visit('dashboard', {
      'api/auth/info': () => ({ ...claims, judgeId: 55, judgeHomeLocationId: 8 }),
      'api/users/me': () => ({ ...databaseUser, judgeId: 55 }),
    });
    await page.router.isReady();
    await flushPromises();

    expect(page.wrapper.text()).toContain('Protected dashboard');
    expect(page.store.userInfo).toMatchObject({ judgeId: 55, judgeHomeLocationId: 8 });
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me', 'api/application/info']);
  });

  it('rejects stale persisted authorization and judge selection after revocation', async () => {
    localStorage.setItem('CommonStore', JSON.stringify({
      isInitialized: true,
      sessionState: SESSION_STATES.AUTHORIZED,
      userInfo: { ...claims, judgeId: 99 },
      loggedInUserInfo: claims,
      appInfo: { version: 'stale' },
      displayName: 'Saved preference',
    }));
    const page = await visit('dashboard', {
      'api/auth/info': () => ({ ...claims, isActive: false, roles: [] }),
      'api/users/me': () => ({ ...databaseUser, isActive: false, roles: [], judgeId: null }),
    });
    await page.router.isReady();
    await flushPromises();

    expect(page.router.currentRoute.value.name).toBe('RequestAccess');
    expect(page.wrapper.text()).toContain('Submit Your Request');
    expect(page.wrapper.text()).not.toContain('Protected profile');
    expect(page.store.loggedInUserInfo).toBeNull();
    expect(page.store.userInfo?.judgeId).toBe(0);
    expect(page.store.appInfo).toBeNull();
    expect(page.store.displayName).toBe('Saved preference');
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me']);
    expect(page.getJudges).not.toHaveBeenCalled();
    expect(page.getOrders).not.toHaveBeenCalled();
    expect(page.startNotifications).not.toHaveBeenCalled();
    expect(JSON.parse(localStorage.getItem('CommonStore')!)).not.toHaveProperty('sessionState');
  });

  it('restores a same-account judge selection and keeps the server judge for reset', async () => {
    const authUser = { ...claims, judgeId: 100 };
    localStorage.setItem('CommonStore', JSON.stringify({
      userInfo: { ...authUser, judgeId: 300, judgeHomeLocationId: 9 },
      loggedInUserInfo: authUser,
    }));
    const page = await visit('dashboard', {
      'api/auth/info': () => authUser,
      'api/users/me': () => ({ ...databaseUser, judgeId: 200 }),
    });
    await page.router.isReady();

    expect(page.store.userInfo).toMatchObject({ judgeId: 300, judgeHomeLocationId: 9 });
    expect(page.store.loggedInUserInfo).toMatchObject({ judgeId: 100, judgeHomeLocationId: 3 });
    expect(page.getOrders).toHaveBeenCalledExactlyOnceWith(300);
  });

  it('uses the fresh mapping when the persisted selection was the authenticated judge', async () => {
    const authUser = { ...claims, judgeId: 100 };
    localStorage.setItem('CommonStore', JSON.stringify({
      userInfo: { ...authUser, judgeHomeLocationId: 9 },
      loggedInUserInfo: authUser,
    }));
    const page = await visit('dashboard', {
      'api/auth/info': () => authUser,
      'api/users/me': () => ({ ...databaseUser, judgeId: 200 }),
    });
    await page.router.isReady();

    expect(page.store.userInfo).toMatchObject({ judgeId: 200, judgeHomeLocationId: 3 });
  });

  it('discards a saved selection when the authenticated judge changes', async () => {
    localStorage.setItem('CommonStore', JSON.stringify({
      userInfo: { ...claims, judgeId: 300, judgeHomeLocationId: 9 },
      loggedInUserInfo: { ...claims, judgeId: 200 },
    }));
    const page = await visit('dashboard', {
      'api/auth/info': () => ({ ...claims, judgeId: 100 }),
      'api/users/me': () => ({ ...databaseUser, judgeId: 200 }),
    });
    await page.router.isReady();

    expect(page.store.userInfo).toMatchObject({ judgeId: 200, judgeHomeLocationId: 3 });
  });

  it('discards the judge selection when the authenticated account changes', async () => {
    localStorage.setItem('CommonStore', JSON.stringify({
      userInfo: { ...claims, userId: 'previous', judgeId: 99 },
      loggedInUserInfo: { ...claims, userId: 'previous' },
    }));
    const page = await visit();
    await page.router.isReady();

    expect(page.store.userInfo).toMatchObject({ userId: claims.userId, judgeId: 12 });
  });

  it('shares in-flight initialization and keeps the loader up while configuration is pending', async () => {
    const identity = deferred<typeof claims>();
    const configuration = deferred<{ version: string }>();
    const page = await visit('dashboard', {
      'api/auth/info': () => identity.promise,
      'api/application/info': () => configuration.promise,
    });
    await vi.waitFor(() => expect(page.urls()).toEqual(['api/auth/info']));
    expect(page.wrapper.text()).toContain('Verifying your JASPER access...');
    const navigation = page.router.push('/court-list');
    identity.resolve(claims);
    await vi.waitFor(() => expect(page.urls()).toEqual([
      'api/auth/info', 'api/users/me', 'api/application/info',
    ]));
    expect(page.wrapper.text()).toContain('Verifying your JASPER access...');
    expect(page.wrapper.text()).not.toContain('Submit Your Request');
    expect(page.getJudges).not.toHaveBeenCalled();

    configuration.resolve({ version: 'test' });
    await navigation;
    await nextTick();
    expect(page.wrapper.text()).toContain('Protected court list');
    expect(page.urls()).toHaveLength(3);
  });

  it('discards a pending result if the session has already failed', async () => {
    const configuration = deferred<{ version: string }>();
    const page = await visit('dashboard', {
      'api/application/info': () => configuration.promise,
    });
    await vi.waitFor(() => expect(page.urls()).toHaveLength(3));
    page.store.clearSession(SESSION_STATES.FAILED);
    configuration.resolve({ version: 'obsolete' });
    await flushPromises();

    await expectFailure(page);
    expect(page.store.appInfo).toBeNull();
    expect(page.store.userInfo).toBeNull();
  });

  it.each([undefined, 500, 403])('shows initialization failure after an API error (%s)', async (status) => {
    const page = await visit('dashboard', { 'api/users/me': rejectRequest(status) });
    await expectFailure(page);
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me']);
    expect(login).not.toHaveBeenCalled();
    expect(logout).not.toHaveBeenCalled();
  });

  it('shows the invalid account warning for an explicit missing user response', async () => {
    const page = await visit('dashboard', { 'api/users/me': rejectRequest(404) });
    await page.router.isReady();
    expect(page.wrapper.text()).toContain('Warning, you do not have valid access to JASPER.');
    expect(page.wrapper.get('v-btn').attributes('disabled')).toBeDefined();
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me']);
  });

  it.each([undefined, 0, -1])('fails initialization without a valid effective judge ID (%s)', async (judgeId) => {
    const authUser = { ...claims, judgeId };
    localStorage.setItem('CommonStore', JSON.stringify({
      userInfo: { ...authUser, judgeId: 99 },
      loggedInUserInfo: authUser,
    }));
    const page = await visit('dashboard', {
      'api/auth/info': () => authUser,
      'api/users/me': () => ({ ...databaseUser, judgeId: null }),
    });
    await expectFailure(page);
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me']);
  });

  it.each([undefined, null, 0])(
    'initializes and preserves the server home location when it is %s',
    async (judgeHomeLocationId) => {
      const { judgeHomeLocationId: _originalHomeLocation, ...authUser } = claims;
      const page = await visit('dashboard', {
        'api/auth/info': () =>
          judgeHomeLocationId === undefined
            ? authUser
            : { ...authUser, judgeHomeLocationId },
        'api/users/me': () => ({ ...databaseUser, judgeId: null }),
      });
      await page.router.isReady();
      await flushPromises();

      expect(page.wrapper.text()).toContain('Protected dashboard');
      expect(page.store.sessionState).toBe(SESSION_STATES.AUTHORIZED);
      expect(page.store.loggedInUserInfo?.judgeHomeLocationId).toBe(judgeHomeLocationId);
      expect(page.store.userInfo?.judgeHomeLocationId).toBe(judgeHomeLocationId);
      expect(page.urls()).toEqual(['api/auth/info', 'api/users/me', 'api/application/info']);
    }
  );

  it('fails initialization if the database response omits resolved roles', async () => {
    const page = await visit('dashboard', {
      'api/users/me': () => ({ ...databaseUser, roles: undefined }),
    });
    await expectFailure(page);
    expect(page.urls()).toEqual(['api/auth/info', 'api/users/me']);
  });

  it('challenges an unauthenticated deep link once and allows an explicit clean retry', async () => {
    const path = 'civil-file/123?fromA2A=true';
    const responses = { 'api/auth/info': rejectRequest(401) };
    const initial = await visit(path, responses);
    const returnUrl = location.href;
    await vi.waitFor(() => expect(login).toHaveBeenCalledExactlyOnceWith(returnUrl));
    expect(initial.urls()).toEqual(['api/auth/info']);

    const failed = await visit(path, responses);
    await expectFailure(failed);
    expect(login).toHaveBeenCalledTimes(1);
    expect(location.search).toContain('authError=authentication_failed');

    const retryButton = failed.wrapper.get('v-btn');
    await retryButton.trigger('click');
    await retryButton.trigger('click');
    expect(login).toHaveBeenCalledTimes(2);
    expect(login).toHaveBeenLastCalledWith(returnUrl);

    const retryFailed = await visit(path, responses);
    await expectFailure(retryFailed);
    expect(login).toHaveBeenCalledTimes(2);
  });

  it('shows the error snackbar when a sign-in retry cannot navigate', async () => {
    const page = await visit('?authError=authentication_failed');
    vi.mocked(location.replace).mockImplementationOnce(() => {
      throw new Error('Navigation blocked');
    });

    await page.wrapper.get('v-btn').trigger('click');

    expect(page.http.snackBarStore).toMatchObject({
      title: 'Unable to sign in to JASPER',
      message: 'Please try signing in again.',
      color: 'error',
      isVisible: true,
    });
  });

  it('uses the error snackbar for an unhandled API failure', async () => {
    const page = await visit('dashboard', { 'api/protected': rejectRequest(500) });
    await page.router.isReady();

    await expect(page.http.get('api/protected')).rejects.toThrow();

    expect(page.http.snackBarStore).toMatchObject({
      title: 'Error',
      message: 'Something went wrong, please contact your Administrator.',
      color: 'error',
      isVisible: true,
    });
  });

  it.each(['pcss_authorization_failed', 'authentication_failed'])(
    'recovers an ordinary API request destination after the %s callback',
    async (marker) => {
      const path = 'civil-file/123?fromA2A=true#detail';
      const page = await visit(path, { 'api/protected': rejectRequest(401) });
      await page.router.isReady();
      const returnUrl = location.href;
      await expect(page.http.get('api/protected')).rejects.toThrow();
      expect(login).toHaveBeenCalledExactlyOnceWith(returnUrl);

      const failed = await visit('?authError=' + marker);
      await expectFailure(failed);
      expect(failed.urls()).toEqual([]);
      expect(login).toHaveBeenCalledTimes(1);

      await failed.wrapper.get('v-btn').trigger('click');
      expect(login).toHaveBeenCalledTimes(2);
      expect(login).toHaveBeenLastCalledWith(returnUrl);
    }
  );

  it('keeps automatic login, logout and explicit retry available when storage throws', async () => {
    const denyStorage = () => { throw new Error('Storage unavailable'); };
    const storage = {
      getItem: vi.fn(denyStorage),
      setItem: vi.fn(denyStorage),
      removeItem: vi.fn(denyStorage),
    };
    vi.stubGlobal('sessionStorage', storage);
    const page = await visit('dashboard', { 'api/protected': rejectRequest(409) });
    await page.router.isReady();
    expect(storage.removeItem).toHaveBeenCalledWith(attemptKey);
    await expect(page.http.get('api/protected')).rejects.toThrow();
    expect(logout).toHaveBeenCalledExactlyOnceWith('/');

    await visit('', { 'api/auth/info': rejectRequest(401) });
    const baseUrl = new URL(base, location.origin).href;
    await vi.waitFor(() => expect(login).toHaveBeenCalledExactlyOnceWith(baseUrl));
    expect(storage.getItem).toHaveBeenCalledWith(attemptKey);
    expect(storage.setItem).toHaveBeenCalled();

    const failed = await visit('?authError=authentication_failed');
    await expectFailure(failed);
    expect(failed.urls()).toEqual([]);
    await failed.wrapper.get('v-btn').trigger('click');
    expect(login).toHaveBeenCalledTimes(2);
    expect(login).toHaveBeenLastCalledWith(baseUrl);
  });

  it.each(['malformed', 'expired'])('starts a new journey when persisted metadata is %s', async (kind) => {
    const path = 'civil-file/123?fromA2A=true#detail';
    const returnUrl = new URL(base + path, location.origin).href;
    sessionStorage.setItem(attemptKey, kind === 'malformed' ? '{' : JSON.stringify({
      returnUrl,
      step: 'login',
      expiresAt: Date.now() - 1,
    }));

    await visit(path, { 'api/auth/info': rejectRequest(401) });
    await vi.waitFor(() => expect(login).toHaveBeenCalledExactlyOnceWith(returnUrl));
  });

  it('uses fresh flags and the new deep link when a previous journey was abandoned', async () => {
    await visit('civil-file/123?fromA2A=true', { 'api/auth/info': rejectRequest(401) });
    await vi.waitFor(() => expect(login).toHaveBeenCalledTimes(1));

    const path = 'civil-file/456?fromA2A=true#detail';
    await visit(path, { 'api/auth/info': rejectRequest(401) });
    const returnUrl = location.href;
    await vi.waitFor(() => expect(login).toHaveBeenCalledTimes(2));
    expect(login).toHaveBeenLastCalledWith(returnUrl);
  });

  it.each(['https://other.example/apps/jasper/', '/apps/jasper-other/'])(
    'rejects a stored retry destination outside the current application (%s)',
    async (path) => {
      sessionStorage.setItem(attemptKey, JSON.stringify({
        returnUrl: new URL(path, location.origin).href,
        step: 'login',
        expiresAt: Date.now() + 60_000,
      }));
      const failed = await visit('?authError=authentication_failed');
      await failed.wrapper.get('v-btn').trigger('click');
      expect(login).toHaveBeenCalledExactlyOnceWith(new URL(base, location.origin).href);
    }
  );

  it.each([true, false])('clears the completed attempt (eligible: %s)', async (isActive) => {
    const path = 'dashboard';
    await visit(path, { 'api/auth/info': rejectRequest(401) });
    await vi.waitFor(() => expect(login).toHaveBeenCalledTimes(1));
    expect(sessionStorage.getItem(attemptKey)).not.toBeNull();

    const page = await visit(path, {
      'api/users/me': () => ({
        ...databaseUser, isActive, roles: isActive ? databaseUser.roles : [],
      }),
    });
    await page.router.isReady();
    expect(page.store.sessionState).toBe(
      isActive ? SESSION_STATES.AUTHORIZED : SESSION_STATES.ACCESS_REQUIRED
    );
    expect(sessionStorage.getItem(attemptKey)).toBeNull();
  });

  it('can start a new journey after initialization when browser storage refuses removal', async () => {
    const savedStorage = sessionStorage;
    vi.stubGlobal('sessionStorage', {
      getItem: savedStorage.getItem.bind(savedStorage),
      setItem: savedStorage.setItem.bind(savedStorage),
      removeItem: () => { throw new Error('Removal unavailable'); },
    });
    const path = 'dashboard';
    await visit(path, { 'api/auth/info': rejectRequest(401) });
    await vi.waitFor(() => expect(login).toHaveBeenCalledTimes(1));

    const page = await visit(path, { 'api/protected': rejectRequest(401) });
    await page.router.isReady();
    expect(JSON.parse(savedStorage.getItem(attemptKey)!)).toBeNull();
    await expect(page.http.get('api/protected')).rejects.toThrow();
    expect(login).toHaveBeenCalledTimes(2);
  });

  it.each([[401, 401], [401, 409], [409, 401]] as const)(
    'deduplicates concurrent ordinary %s and %s responses',
    async (firstStatus, secondStatus) => {
      const page = await visit('dashboard', {
        'api/first': rejectRequest(firstStatus),
        'api/second': rejectRequest(secondStatus),
      });
      await page.router.isReady();
      const results = await Promise.allSettled([
        page.http.get('api/first'),
        page.http.get('api/second'),
      ]);

      expect(results.every(({ status }) => status === 'rejected')).toBe(true);
      expect(login.mock.calls.length + logout.mock.calls.length).toBe(1);
      expect(page.store.sessionState).toBe(SESSION_STATES.AUTHORIZED);
    }
  );

  it('keeps startup on the loading screen when an ordinary request has already started redirecting', async () => {
    const startup = deferred<void>();
    const page = await visit('dashboard', {
      'api/auth/info': async (config) => {
        await startup.promise;
        rejectRequest(401)(config);
      },
      'api/protected': rejectRequest(401),
    });
    await vi.waitFor(() => expect(page.urls()).toEqual(['api/auth/info']));
    await expect(page.http.get('api/protected')).rejects.toThrow();
    startup.resolve(undefined);
    await flushPromises();

    expect(login).toHaveBeenCalledTimes(1);
    expect(page.store.sessionState).toBe(SESSION_STATES.INITIALIZING);
    expect(page.wrapper.text()).toContain('Verifying your JASPER access...');
    expect(location.search).toBe('');
  });

  it('shows the failure screen when an ordinary response exhausts recovery on a fresh page', async () => {
    await visit('dashboard', { 'api/auth/info': rejectRequest(401) });
    await vi.waitFor(() => expect(login).toHaveBeenCalledTimes(1));

    const startup = deferred<void>();
    const page = await visit('dashboard', {
      'api/auth/info': () => startup.promise.then(() => claims),
      'api/protected': rejectRequest(401),
    });
    await expect(page.http.get('api/protected')).rejects.toThrow();
    startup.resolve(undefined);
    await expectFailure(page);
    expect(login).toHaveBeenCalledTimes(1);
  });

  it('blocks repeated logout at the base path and resets recovery restrictions on explicit retry', async () => {
    const path = 'civil-file/123?fromA2A=true#detail';
    const responses = { 'api/auth/info': rejectRequest(409) };
    await visit(path, responses);
    const returnUrl = location.href;
    await vi.waitFor(() => expect(logout).toHaveBeenCalledExactlyOnceWith('/'));

    const failed = await visit('', responses);
    await expectFailure(failed);
    expect(logout).toHaveBeenCalledTimes(1);
    await failed.wrapper.get('v-btn').trigger('click');
    expect(login).toHaveBeenCalledExactlyOnceWith(returnUrl);

    await visit(path, responses);
    await vi.waitFor(() => expect(logout).toHaveBeenCalledTimes(2));
  });

  it.each([false, true])('refreshes an already-authorized access request (stale claims: %s)', async (staleClaims) => {
    let submitted = false;
    const requestAccess = vi.fn(() => {
      submitted = true;
      return { alreadyAuthorized: true };
    });
    const page = await visit('dashboard', {
      'api/auth/info': (config) => {
        if (staleClaims && submitted) rejectRequest(409)(config);
        return claims;
      },
      'api/users/me': () => submitted
        ? databaseUser
        : { ...databaseUser, isActive: false, roles: [] },
      'api/users/request-access': requestAccess,
    });
    await page.router.isReady();
    await page.wrapper.get('v-btn').trigger('click');

    if (staleClaims) {
      await vi.waitFor(() => expect(logout).toHaveBeenCalledExactlyOnceWith('/'));
      expect(page.store.sessionState).toBe(SESSION_STATES.INITIALIZING);
      expect(page.wrapper.text()).not.toContain('Your request has been submitted!');
      expect(page.urls()).toEqual([
        'api/auth/info', 'api/users/me', 'api/users/request-access', 'api/auth/info',
      ]);

      const afterLogout = await visit('', { 'api/auth/info': rejectRequest(401) });
      await vi.waitFor(() => expect(login).toHaveBeenCalledExactlyOnceWith(
        new URL(base + 'request-access', location.origin).href
      ));
      expect(afterLogout.urls()).toEqual(['api/auth/info']);
      const afterLogin = await visit('request-access', { 'api/auth/info': rejectRequest(409) });
      await expectFailure(afterLogin);
      expect(logout).toHaveBeenCalledTimes(1);
    } else {
      await vi.waitFor(() => expect(page.router.currentRoute.value.name).toBe('DashboardView'));
      expect(page.wrapper.text()).toContain('Protected dashboard');
      expect(page.wrapper.text()).not.toContain('Your request has been submitted!');
      expect(page.urls()).toEqual([
        'api/auth/info', 'api/users/me', 'api/users/request-access',
        'api/auth/info', 'api/users/me', 'api/application/info',
      ]);
    }
    expect(requestAccess).toHaveBeenCalledTimes(1);
  });
});
