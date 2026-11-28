import { useCommonStore } from '@/stores';
import { PERMISSIONS } from '@/constants/permissions';
import { SESSION_STATES } from '@/constants/sessionStates';
import { callTrackPageView } from '@/utils/snowplowUtils';
import { useSessionInitializer } from '@/utils/utils';
import {
  createRouter,
  createWebHistory,
  RouteLocationNormalizedGeneric,
  RouteRecordRaw,
} from 'vue-router';

function authGuard(to: RouteLocationNormalizedGeneric) {
  const commonStore = useCommonStore();

  if (commonStore.sessionState === SESSION_STATES.ACCESS_REQUIRED) {
    return to.name === 'RequestAccess' ? true : { path: '/request-access' };
  }

  if (commonStore.sessionState !== SESSION_STATES.AUTHORIZED) return false;

  if (to.name === 'RequestAccess') {
    return { path: '/' };
  }

  const requiredPermission = to.meta?.requiredPermission as string | undefined;
  if (
    requiredPermission &&
    !commonStore.userInfo?.permissions?.includes(requiredPermission)
  ) {
    return { path: '/court-list' };
  }

  return true;
}

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    redirect: '/dashboard',
    name: 'HomeView',
  },
  {
    path: '/court-list',
    name: 'CourtList',
    component: () => import('@/components/courtlist/CourtList.vue'),
    props: true,
    children: [
      {
        path: 'location/:location/room/:room/date/:date',
        name: 'CourtListResult',
        component: () => import('@/components/courtlist/CourtList.vue'),
        props: true,
      },
    ],
  },
  {
    path: '/civil-file/:fileNumber/:section?',
    name: 'CivilCaseDetails',
    component: () => import('@/components/civil/CivilCaseDetails.vue'),
    props: true,
  },
  {
    path: '/criminal-file/:fileNumber',
    name: 'CriminalCaseDetails',
    component: () => import('@/components/criminal/CriminalCaseDetails.vue'),
    props: true,
  },
  {
    path: '/dashboard',
    name: 'DashboardView',
    component: () => import('@/components/dashboard/Dashboard.vue'),
    props: true,
  },
  {
    path: '/court-file-search',
    name: 'CourtFileSearchView',
    component: () =>
      import('@/components/courtfilesearch/CourtFileSearchView.vue'),
  },
  {
    path: '/transitory-documents/location/:locationId/room/:roomCd/date/:date',
    name: 'TransitoryDocuments',
    component: () =>
      import('@/components/courtlist/TransitoryDocumentsPage.vue'),
    props: (route) => ({
      locationId: route.params.locationId,
      roomCd: route.params.roomCd,
      date: route.params.date,
      location: route.query.location ?? '',
    }),
    meta: {
      requiredPermission: PERMISSIONS.LIST_TRANSITORY_DOCUMENTS,
    },
  },
  {
    path: '/file-viewer',
    name: 'NutrientContainer',
    component: () => import('@/components/documents/NutrientContainer.vue'),
  },
  {
    path: '/request-access',
    name: 'RequestAccess',
    component: () => import('@/components/dashboard/RequestAccess.vue'),
  },
  {
    path: '/orders',
    name: 'Orders',
    component: () => import('@/components/orders/Orders.vue'),
  },
  {
    path: '/desk-orders',
    name: 'DeskOrders',
    component: () => import('@/components/orders/Orders.vue'),
  },
];

export function createAppRouter() {
  const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes,
  });

  router.beforeEach(async (to) => {
    const commonStore = useCommonStore();
    if (commonStore.sessionState === SESSION_STATES.INITIALIZING) {
      const initializeSessionSettings = useSessionInitializer();
      await initializeSessionSettings();
    }
    return authGuard(to);
  });

  router.afterEach(() => {
    callTrackPageView();
  });

  return router;
}
