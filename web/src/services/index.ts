import { App } from 'vue';
import { ApplicationService } from './ApplicationService';
import { AuthService } from './AuthService';
import { BinderService } from './BinderService';
import { CaseService } from './CaseService';
import { CourtListService } from './CourtListService';
import { DarsService } from './DarsService';
import { DashboardService } from './DashboardService';
import { FilesService } from './FilesService';
import { HttpService } from './HttpService';
import { LocationService } from './LocationService';
import { LookupService } from './LookupService';
import { TimebankService } from './TimebankService';
import { TransitoryDocumentsService } from './TransitoryDocumentsService';
import { UserService } from './UserService';
import { QuickLinkService } from './QuickLinkService';

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
  const caseService = new CaseService(httpService);
  const timebankService = new TimebankService(httpService);
  const darsService = new DarsService(httpService);
  const quickLinkService = new QuickLinkService(httpService);
  const transitoryDocumentsService = new TransitoryDocumentsService(
    httpService
  );

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
  app.provide('caseService', caseService);
  app.provide('timebankService', timebankService);
  app.provide('transitoryDocumentsService', transitoryDocumentsService);
  app.provide('darsService', darsService);
  app.provide('quickLinkService', quickLinkService);
}

export * from './AuthService';
export * from './BinderService';
export * from './CaseService';
export * from './CourtListService';
export * from './DarsService';
export * from './DashboardService';
export * from './FilesService';
export * from './LocationService';
export * from './LookupService';
export * from './RedirectHandlerService';
export * from './TimebankService';
export * from './TransitoryDocumentsService';
export * from './UserService';
export * from './QuickLinkService';
