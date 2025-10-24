import { App } from 'vue';
import { ApplicationService } from './ApplicationService';
import { AuthService } from './AuthService';
import { BinderService } from './BinderService';
import { CourtListService } from './CourtListService';
import { DashboardService } from './DashboardService';
import { FilesService } from './FilesService';
import { HttpService } from './HttpService';
import { LocationService } from './LocationService';
import { LookupService } from './LookupService';
import { ReservedJudgementService } from './ReservedJudgementService';
import { UserService } from './UserService';

export function registerRouter(app: App) {
  const httpService = new HttpService(import.meta.env.BASE_URL);
  const authService = new AuthService(httpService);
  const applicationService = new ApplicationService(httpService);
  const lookupService = new LookupService(httpService);
  const locationService = new LocationService(httpService);
  const filesService = new FilesService(httpService);
  const dashboardService = new DashboardService(httpService);
  const courtListService = new CourtListService(httpService);
  const binderService = new BinderService(httpService);
  const userService = new UserService(httpService);
  const reservedJudgementService = new ReservedJudgementService(httpService);

  app.provide('httpService', httpService);
  app.provide('authService', authService);
  app.provide('applicationService', applicationService);
  app.provide('lookupService', lookupService);
  app.provide('locationService', locationService);
  app.provide('filesService', filesService);
  app.provide('dashboardService', dashboardService);
  app.provide('courtListService', courtListService);
  app.provide('binderService', binderService);
  app.provide('userService', userService);
  app.provide('reservedJudgementService', reservedJudgementService);
}

export * from './AuthService';
export * from './BinderService';
export * from './CourtListService';
export * from './DashboardService';
export * from './FilesService';
export * from './LocationService';
export * from './LookupService';
export * from './RedirectHandlerService';
export * from './UserService';
