import { FilesService } from '../services/FilesService';
import { HttpService } from '../services/HttpService';
import { LocationService } from '../services/LocationService';
import { LookupService } from '../services/LookupService';

// TODO: Change this to use Pinia

declare module '@vue/runtime-core' {
  interface ComponentCustomProperties {
    $lookupService: LookupService;
    $locationService: LocationService;
    $filesService: FilesService;
  }
}

export default {
  install(app: any) {
    const httpService = new HttpService();
    const lookupService = new LookupService(httpService);
    const locationService = new LocationService(httpService);
    const filesService = new FilesService(httpService);

    // Inject into Vue's prototype so it can be accessed from any component
    app.config.globalProperties.$lookupService = lookupService;
    app.config.globalProperties.$locationService = locationService;
    app.config.globalProperties.$filesService = filesService;
    app.config.globalProperties.$httpService = httpService;
  }
};
