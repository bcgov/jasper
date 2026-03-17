import LoadingSpinner from '@/components/LoadingSpinner.vue';
import { registerPlugins } from '@/plugins';
import 'bootstrap/dist/css/bootstrap.css';
import 'intersection-observer';
import { createApp } from 'vue';
import App from './App.vue';
import './assets/colors.css';
import './filters';
import router from './router/index';
import { registerRouter } from './services';
import pinia, { registerPinia } from './stores';
import { callRefreshLinkClickTracking } from '@/utils/snowplowUtils';
import { useNotificationsStore } from './stores/NotificationsStore';

const app = createApp(App);
registerPinia(app);
app.use(router);
//Vue.config.productionTip = true
app.component('loading-spinner', LoadingSpinner);

const services = registerRouter(app);
registerPlugins(app);

const notificationsStore = useNotificationsStore(pinia);
notificationsStore.initialize(
  services.notificationsService,
  services.orderService
);
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

// Redirect from / to /jasper/
if (location.pathname == '/') {
  history.pushState({ page: 'home' }, '', import.meta.env.BASE_URL);
}
