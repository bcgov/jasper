import { SESSION_STATES } from '@/constants/sessionStates';
import { registerPlugins } from '@/plugins';
import { callRefreshLinkClickTracking } from '@/utils/snowplowUtils';
import '@fullcalendar/vue3/skeleton.css';
import '@fullcalendar/vue3/themes/classic/palette.css';
import '@fullcalendar/vue3/themes/classic/theme.css';
import 'bootstrap/dist/css/bootstrap.css';
import './assets/print.css';
import 'intersection-observer';
import { createApp } from 'vue';
import App from './App.vue';
import './assets/colors.css';
import './filters';
import { createAppRouter } from './router/index';
import { registerRouter } from './services';
import redirectHandlerService from './services/RedirectHandlerService';
import { registerPinia, useCommonStore } from './stores';

const bootstrap = async () => {
  const authenticationFailed =
    redirectHandlerService.hasAuthenticationFailure();
  const app = createApp(App);
  registerPinia(app);

  const commonStore = useCommonStore();
  if (authenticationFailed) {
    commonStore.clearSession(SESSION_STATES.FAILED);
  } else {
    commonStore.sessionState = SESSION_STATES.INITIALIZING;
  }
  registerRouter(app);
  registerPlugins(app);

  if (!authenticationFailed) {
    if (location.pathname === '/') {
      const url = new URL(location.href);
      url.pathname = import.meta.env.BASE_URL;
      history.replaceState(history.state, '', url);
    }
    app.use(createAppRouter());
  }
  app.mount('#app');

  // Initialize Snowplow link click tracking after app mounts
  callRefreshLinkClickTracking();

  // Debounce helper to limit how often the refresh is called
  const debounce = (fn: () => void, delay: number) => {
    let timeoutId: ReturnType<typeof setTimeout>;
    return () => {
      clearTimeout(timeoutId);
      timeoutId = setTimeout(() => fn(), delay);
    };
  };

  // Set up MutationObserver to refresh link tracking when DOM changes
  // Debounced to avoid excessive calls in SPA with frequent DOM updates
  const debouncedRefresh = debounce(callRefreshLinkClickTracking, 300);
  const observer = new MutationObserver(debouncedRefresh);

  observer.observe(document.body, {
    childList: true,
    subtree: true,
  });
};

try {
  await bootstrap();
} catch (error) {
  console.error('Failed to bootstrap application.', error);
  const commonStore = useCommonStore();
  commonStore.clearSession(SESSION_STATES.FAILED);
  if (!redirectHandlerService.hasAuthenticationFailure()) {
    redirectHandlerService.markInitializationFailed();
  }
}
