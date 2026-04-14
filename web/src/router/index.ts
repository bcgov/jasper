import { useCommonStore } from '@/stores';
import { callTrackPageView } from '@/utils/snowplowUtils';
import { initializeSessionSettings, isPositiveInteger } from '@/utils/utils';
import {
  createRouter,
  createWebHistory,
  RouteLocationNormalizedGeneric,
  RouteRecordRaw,
} from 'vue-router';

function authGuard(to: RouteLocationNormalizedGeneric) {
  const commonStore = useCommonStore();

  // Check user's access control only when user data is fully loaded (commonStore.isInitializing is false).
  const isAuthorized =
    isPositiveInteger(commonStore.userInfo?.roles?.length) &&
    commonStore.userInfo?.isActive === true &&
    !!commonStore.userInfo?.judgeId;

  if (!isAuthorized) {
    return to.name === 'RequestAccess' ? true : { path: '/request-access' };
  }

  if (to.name === 'RequestAccess') {
    return { path: '/' };
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
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

router.beforeEach(async (to) => {
  const commonStore = useCommonStore();

  // Initialize session settings if not already initialized. This
  // ensures that user data is loaded before performing auth
  // checks in authGuard.
  if (!commonStore.isInitialized) {
    await initializeSessionSettings();
  }

  if (to.path === '/') {
    return true;
  } else {
    return authGuard(to);
  }
});

router.afterEach(() => {
  callTrackPageView();
});

export default router;
