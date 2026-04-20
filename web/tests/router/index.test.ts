import { beforeEach, describe, expect, it, vi } from 'vitest';

type RouteResult = { path: string } | boolean | undefined;
type RouteLike = { path: string; name: string };

interface RouterHooks {
  beforeEach?: (
    to: RouteLike,
    from: unknown
  ) => Promise<RouteResult> | RouteResult;
  afterEach?: () => void;
}

interface MockStore {
  isInitialized: boolean;
  userInfo: {
    roles: string[];
    isActive: boolean;
    judgeId: number | null;
  };
}

describe('router/index', () => {
  const hooks: RouterHooks = {};

  let mockStore: MockStore;
  const callTrackPageView = vi.fn();
  const initializeSessionSettings = vi.fn().mockResolvedValue(true);

  const loadRouter = async () => {
    vi.resetModules();
    hooks.beforeEach = undefined;
    hooks.afterEach = undefined;

    vi.doMock('@/stores', () => ({
      useCommonStore: () => mockStore,
    }));

    vi.doMock('@/utils/snowplowUtils', () => ({
      callTrackPageView,
    }));

    vi.doMock('@/utils/utils', () => ({
      initializeSessionSettings,
      isPositiveInteger: (n: number) => Number.isInteger(n) && n > 0,
    }));

    vi.doMock('vue-router', () => ({
      createWebHistory: vi.fn(),
      createRouter: vi.fn(() => ({
        beforeEach: (cb: RouterHooks['beforeEach']) => {
          hooks.beforeEach = cb;
        },
        afterEach: (cb: () => void) => {
          hooks.afterEach = cb;
        },
      })),
    }));

    await import('@/router/index');
  };

  beforeEach(() => {
    mockStore = {
      isInitialized: false,
      userInfo: {
        roles: ['role-1'],
        isActive: true,
        judgeId: 123,
      },
    };
    callTrackPageView.mockClear();
    initializeSessionSettings.mockClear();
  });

  it('allows root path without auth guard', async () => {
    await loadRouter();

    const result = await hooks.beforeEach?.(
      { path: '/', name: 'HomeView' },
      {}
    );

    expect(result).toBe(true);
  });

  it('redirects to request access when user is unauthorized', async () => {
    mockStore.userInfo = {
      roles: [],
      isActive: true,
      judgeId: null,
    };

    await loadRouter();

    const result = await hooks.beforeEach?.(
      { path: '/dashboard', name: 'DashboardView' },
      {}
    );

    expect(result).toEqual({ path: '/request-access' });
  });

  it('allows navigation to request access for unauthorized user', async () => {
    mockStore.userInfo = {
      roles: [],
      isActive: false,
      judgeId: null,
    };

    await loadRouter();

    const result = await hooks.beforeEach?.(
      { path: '/request-access', name: 'RequestAccess' },
      {}
    );

    expect(result).toBe(true);
  });

  it('redirects authorized user away from request access', async () => {
    await loadRouter();

    const result = await hooks.beforeEach?.(
      { path: '/request-access', name: 'RequestAccess' },
      {}
    );

    expect(result).toEqual({ path: '/' });
  });

  it('allows authorized user to access other routes', async () => {
    await loadRouter();

    const result = await hooks.beforeEach?.(
      { path: '/dashboard', name: 'DashboardView' },
      {}
    );

    expect(result).toBe(true);
  });

  it('allows navigation when store is already initialized', async () => {
    mockStore.isInitialized = true;

    await loadRouter();

    const result = await hooks.beforeEach?.(
      { path: '/dashboard', name: 'DashboardView' },
      {}
    );

    expect(result).toBe(true);
  });

  it('tracks page view on afterEach hook', async () => {
    await loadRouter();

    hooks.afterEach?.();

    expect(callTrackPageView).toHaveBeenCalledTimes(1);
  });

  it('calls initializeSessionSettings when store is not yet initialized', async () => {
    mockStore.isInitialized = false;

    await loadRouter();
    await hooks.beforeEach?.({ path: '/dashboard', name: 'DashboardView' }, {});

    expect(initializeSessionSettings).toHaveBeenCalledTimes(1);
  });

  it('skips initializeSessionSettings when store is already initialized', async () => {
    mockStore.isInitialized = true;

    await loadRouter();
    await hooks.beforeEach?.({ path: '/dashboard', name: 'DashboardView' }, {});

    expect(initializeSessionSettings).not.toHaveBeenCalled();
  });
});
