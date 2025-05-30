import { SessionManager } from '@/utils/utils';
import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';

async function authGuard(to: any, from: any, next: any) {
  const results = await SessionManager.getSettings();
  if (results) {
    next();
  }
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
    path: '/civil-file-search',
    name: 'CivilFileSearchResultsView',
    component: () =>
      import('@/components/civil/CivilFileSearchResultsView.vue'),
    props: true,
  },
  {
    path: '/criminal-file-search',
    name: 'CriminalFileSearchResultsView',
    component: import(
      '@/components/criminal/CriminalFileSearchResultsView.vue'
    ),
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
    path: '/pdf-viewer',
    name: 'NutrientContainer',
    component: () => import('@/components/pdf/NutrientContainer.vue'),
  },
];

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes,
});

router.beforeEach((to, from, next) => {
  if (to.path === '/') {
    next();
  } else {
    authGuard(to, from, next);
  }
});

export default router;
