import LoadingSpinner from '@components/LoadingSpinner.vue';
//import "@styles/index.scss"
import axios from 'axios';
import 'core-js/stable';
import 'intersection-observer';
import 'regenerator-runtime/runtime';
import { createApp } from 'vue';
import App from './App.vue';
import './filters';
import ServicePlugin from './plugins/ServicePlugin';
import router from './router/index';
import { HttpService } from './services/HttpService';
import { registerPinia } from './stores';

// Configure Axios as needed
axios.defaults.baseURL = process.env.BASE_URL;
axios.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      window.location.replace(
        `${process.env.BASE_URL}api/auth/login?redirectUri=${window.location}`
      );
    }
    return Promise.reject(error);
  }
);

// Create Vue App
const app = createApp(App);

// Plugins
//app.use(VueResource);
registerPinia(app);
app.use(router);
//app.use(BootstrapVue);
//app.use(BootstrapVueIcons);
app.use(ServicePlugin);

const httpService = new HttpService(process.env.API_URL);
app.provide('httpService', httpService);

app.component('LoadingSpinner', LoadingSpinner);

app.mount('#app');

// Redirect from / to /jasper/
if (location.pathname == '/') {
  history.pushState({ page: 'home' }, '', process.env.BASE_URL);
}
