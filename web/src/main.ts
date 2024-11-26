import LoadingSpinner from '@components/LoadingSpinner.vue';
import '@styles/index.scss';
import { createApp } from '@vue/compat';
import axios from 'axios';
import BootstrapVue, { BootstrapVueIcons } from 'bootstrap-vue';
import 'core-js/stable';
import 'intersection-observer';
import 'regenerator-runtime/runtime';
import App from './App.vue';
import './filters';
import router from './router/index';
import { FilesService } from './services/FilesService';
import { HttpService } from './services/HttpService';
import { LocationService } from './services/LocationService';
import { LookupService } from './services/LookupService';
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
app.use(BootstrapVue);
app.use(BootstrapVueIcons);

// Setup services
const httpService = new HttpService(process.env.API_URL!);
const lookupService = new LookupService(httpService);
const locationService = new LocationService(httpService);
const filesService = new FilesService(httpService);

app.provide('httpService', httpService);
app.provide('lookupService', lookupService);
app.provide('locationService', locationService);
app.provide('filesService', filesService);

app.component('LoadingSpinner', LoadingSpinner);

app.mount('#app');

// Redirect from / to /jasper/
if (location.pathname == '/') {
  history.pushState({ page: 'home' }, '', process.env.BASE_URL);
}
