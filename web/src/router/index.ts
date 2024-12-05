import CivilCaseDetails from '@/components/civil/CivilCaseDetails.vue';
import CivilFileSearchResultsView from '@/components/civil/CivilFileSearchResultsView.vue';
import CourtFileSearchView from '@/components/courtfilesearch/CourtFileSearchView.vue';
import CourtList from '@/components/courtlist/CourtList.vue';
import CriminalCaseDetails from '@/components/criminal/CriminalCaseDetails.vue';
import CriminalFileSearchResultsView from '@/components/criminal/CriminalFileSearchResultsView.vue';
import Dashboard from '@/components/dashboard/Dashboard.vue';
import { SessionManager } from '@/utils/utils';
import { createRouter, createWebHistory, RouteRecordRaw } from 'vue-router';

async function authGuard(to: any, from: any, next: any) {
  const results = await SessionManager.getSettings();
  if (results) {
    next();
  }
}

const HomeView = () => import('../components/Home.vue');

const routes: RouteRecordRaw[] = [
  {
    path: '/',
    name: 'HomeView',
    component: HomeView,
  },
  {
    path: '/court-list',
    name: 'CourtList',
    component: CourtList,
    props: true,
    children: [
      {
        path: 'location/:location/room/:room/date/:date',
        name: 'CourtListResult',
        component: CourtList,
        props: true,
      },
    ],
  },
  {
    path: '/civil-file/:fileNumber/:section?',
    name: 'CivilCaseDetails',
    component: CivilCaseDetails,
    props: true,
  },
  {
    path: '/criminal-file/:fileNumber',
    name: 'CriminalCaseDetails',
    component: CriminalCaseDetails,
    props: true,
  },
  {
    path: '/civil-file-search',
    name: 'CivilFileSearchResultsView',
    component: CivilFileSearchResultsView,
    props: true,
  },
  {
    path: '/criminal-file-search',
    name: 'CriminalFileSearchResultsView',
    component: CriminalFileSearchResultsView,
    props: true,
  },
  {
    path: '/dashboard',
    name: 'DashboardView',
    component: Dashboard,
    props: true,
  },
  {
    path: '/court-file-search',
    name: 'CourtFileSearchView',
    component: CourtFileSearchView,
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
