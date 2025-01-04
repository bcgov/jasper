import { App } from 'vue';
import { AuthService } from './AuthService';
import { DashboardService } from './DashboardService';
import { FilesService } from './FilesService';
import { HttpService } from './HttpService';
import { LocationService } from './LocationService';
import { LookupService } from './LookupService';

export function registerRouter(app: App) {
  const httpService = new HttpService(import.meta.env.BASE_URL);
  const authService = new AuthService(httpService);
  const lookupService = new LookupService(httpService);
  const locationService = new LocationService(httpService);
  const filesService = new FilesService(httpService);
  const dashboardService = new DashboardService(httpService);

  app.provide('httpService', httpService);
  app.provide('authService', authService);
  app.provide('lookupService', lookupService);
  app.provide('locationService', locationService);
  app.provide('filesService', filesService);
  app.provide('dashboardService', dashboardService);
}

export * from './AuthService';
export * from './DashboardService';
export * from './FilesService';
export * from './LocationService';
export * from './LookupService';
export * from './RedirectHandlerService';
