import LoadingSpinner from "@components/LoadingSpinner.vue"
import "@styles/index.scss"
import { BootstrapVue, BootstrapVueIcons } from 'bootstrap-vue'
import 'core-js/stable'
import 'intersection-observer'
import 'regenerator-runtime/runtime'
import Vue from 'vue'
import VueResource from 'vue-resource'
import VueRouter from 'vue-router'
import App from './App.vue'
import "./filters"
import ServicePlugin from './plugins/ServicePlugin'
import routes from './router/index'
import store from './store/index'

Vue.use(VueResource);
Vue.use(VueRouter);
Vue.use(BootstrapVue);
Vue.use(BootstrapVueIcons);
Vue.use(ServicePlugin);
Vue.config.productionTip = true;
Vue.component('loading-spinner', LoadingSpinner);

Vue.http.interceptors.push(function () {
	return function (response) {
		if (response.status == 401) {
			location.replace(`${process.env.BASE_URL}api/auth/login?redirectUri=${window.location}`);
		}
	};
});

Vue.http.options.root = process.env.BASE_URL;

// Redirect from / to /jasper/
if (location.pathname == "/")
	history.pushState({ page: "home" }, "", process.env.BASE_URL);

const router = new VueRouter({
	mode: 'history',
	base: process.env.BASE_URL,
	routes: routes
});

new Vue({
	router,
	store,
	render: h => h(App)
}).$mount('#app');
