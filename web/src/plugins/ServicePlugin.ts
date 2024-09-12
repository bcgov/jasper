import { FilesService } from "../services/FilesService";
import { HttpService } from "../services/HttpService";
import { LocationService } from "../services/LocationService";
import { LookupService } from "../services/LookupService";

declare module 'vue/types/vue' {
  interface Vue {
    $lookupService: LookupService;
    $locationService: LocationService;
    $filesService: FilesService;
  }
}

export default {
  install(Vue: any) {
    const httpService = new HttpService();
    const lookupService = new LookupService(httpService);
    const locationService = new LocationService(httpService);
    const filesService = new FilesService(httpService);

    // Inject into Vue's prototype so it can be accessed from any component
    Vue.prototype.$lookupService = lookupService;
    Vue.prototype.$locationService = locationService;
    Vue.prototype.$filesService = filesService;
  }
};