import { PERMISSIONS } from '@/constants/permissions';
import { SESSION_STATES } from '@/constants/sessionStates';
import { createAppRouter } from '@/router';
import { useCommonStore } from '@/stores/CommonStore';
import { callTrackPageView } from '@/utils/snowplowUtils';
import type { UserInfo } from '@/types/common';
import { createPinia } from 'pinia';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { createApp } from 'vue';

vi.mock('@/utils/snowplowUtils');
vi.mock('@/components/dashboard/Dashboard.vue', () => ({ default: {} }));
vi.mock('@/components/courtlist/CourtList.vue', () => ({ default: {} }));
vi.mock('@/components/courtlist/TransitoryDocumentsPage.vue', () => ({ default: {} }));

describe('JASPER router', () => {
  const documentRoute = '/transitory-documents/location/1/room/1/date/2025-11-01';
  let router: ReturnType<typeof createAppRouter>;
  let store: ReturnType<typeof useCommonStore>;

  beforeEach(async () => {
    vi.stubEnv('BASE_URL', '/');
    history.replaceState(null, '', '/dashboard');
    const pinia = createPinia();
    const app = createApp({});
    app.use(pinia);
    store = useCommonStore(pinia);
    store.sessionState = SESSION_STATES.AUTHORIZED;
    router = createAppRouter();
    app.use(router);
    await router.isReady();
  });

  afterEach(() => {
    router.options.history.destroy();
    vi.unstubAllEnvs();
  });

  it('redirects a user who lacks the route permission to court list', async () => {
    store.userInfo = { permissions: [] } as unknown as UserInfo;
    await router.push(documentRoute);
    expect(router.currentRoute.value.name).toBe('CourtList');
  });

  it('keeps the requested route when the user has the required permission', async () => {
    store.userInfo = {
      permissions: [PERMISSIONS.LIST_TRANSITORY_DOCUMENTS],
    } as unknown as UserInfo;
    await router.push(documentRoute);
    expect(router.currentRoute.value.name).toBe('TransitoryDocuments');
    expect(callTrackPageView).toHaveBeenCalled();
  });

  it('redirects an authorized user away from the access request route', async () => {
    await router.push('/request-access');
    expect(router.currentRoute.value.name).toBe('DashboardView');
  });
});
