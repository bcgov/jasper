import { AxiosRequestConfig } from 'axios';

export interface CustomAxiosConfig extends AxiosRequestConfig {
  skipErrorHandler?: boolean;
  suppressErrorNotification?: boolean;
  suppressConflictLogout?: boolean;
}
