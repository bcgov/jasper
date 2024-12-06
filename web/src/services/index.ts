import { App } from 'vue';
import { AuthService } from './AuthService';
import { FilesService } from './FilesService';
import { HttpService } from './HttpService';
import { LocationService } from './LocationService';
import { LookupService } from './LookupService';

export function registerRouter(app: App) {
  console.log(import.meta.env);

  const httpService = new HttpService(import.meta.env.BASE_URL);
  const authService = new AuthService(httpService);
  const lookupService = new LookupService(httpService);
  const locationService = new LocationService(httpService);
  const filesService = new FilesService(httpService);

  app.provide('httpService', httpService);
  app.provide('authService', authService);
  app.provide('lookupService', lookupService);
  app.provide('locationService', locationService);
  app.provide('filesService', filesService);
}
