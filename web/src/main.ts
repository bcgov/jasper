import LoadingSpinner from '@components/LoadingSpinner.vue';
import '@styles/index.scss';
import { createApp } from '@vue/compat';
import { BootstrapVue, BootstrapVueIcons } from 'bootstrap-vue';
import 'intersection-observer';
import App from './App.vue';
import './filters';
import router from './router/index';
import { registerRouter } from './services';
import { registerPinia } from './stores';

const app = createApp(App);

registerPinia(app);
app.use(router);
app.use(BootstrapVue);
app.use(BootstrapVueIcons);
//Vue.config.productionTip = true
app.component('loading-spinner', LoadingSpinner);

registerRouter(app);

app.mount('#app');

// Redirect from / to /jasper/
if (location.pathname == '/') {
  history.pushState({ page: 'home' }, '', import.meta.env.BASE_URL);
}
