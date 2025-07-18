import { useSnackbarStore } from '@/stores/SnackbarStore';
import axios, {
  AxiosInstance,
  AxiosRequestConfig,
  InternalAxiosRequestConfig,
} from 'axios';
import redirectHandlerService from './RedirectHandlerService';

export interface IHttpService {
  get<T>(resource: string, queryParams?: Record<string, any>): Promise<T>;
  post<T>(
    resource: string,
    data: any,
    headers?: Record<string, string>,
    responseType?: 'json' | 'blob'
  ): Promise<T>;
  put<T>(
    resource: string,
    data: any,
    headers?: Record<string, string>,
    responseType?: 'json' | 'blob'
  ): Promise<T>;
  delete<T>(
    resource: string,
    headers?: Record<string, string>,
    responseType?: 'json' | 'blob'
  ): Promise<T>;
}

export class HttpService implements IHttpService {
  readonly client: AxiosInstance;
  snackBarStore = useSnackbarStore();

  constructor(baseURL: string) {
    this.client = axios.create({
      baseURL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    this.client.interceptors.request.use(
      (config) => this.handleAuthSuccess(config),
      (error) => Promise.reject(new Error(error))
    );

    this.client.interceptors.response.use(
      (response) => response,
      (error) => this.handleAuthError(error)
    );
  }

  private handleAuthSuccess(
    config: InternalAxiosRequestConfig
  ): InternalAxiosRequestConfig {
    return config;
  }

  private handleAuthError(error: any) {
    console.error(error);
    console.log('User unauthenticated.');
    // todo: check for a 403 and handle it
    if (error.response && error.response.status === 401) {
      redirectHandlerService.handleUnauthorized(window.location.href);
    } else {
      // The user should be notified about unhandled server exceptions.
      this.snackBarStore.showSnackbar(
        'Something went wrong, please contact your Administrator.',
        '#b84157',
        'Error'
      );
    }
    return Promise.reject(new Error(error));
  }

  public async get<T>(
    resource: string,
    queryParams: Record<string, any> = {}
  ): Promise<T> {
    try {
      const response = await this.client.get<T>(resource, {
        params: queryParams,
      });
      return response.data;
    } catch (error) {
      console.error('Error in GET request: ', error);
      throw error;
    }
  }

  public async post<T>(
    resource: string,
    data: any,
    headers: Record<string, string> = {},
    responseType: 'json' | 'blob' = 'json'
  ): Promise<T> {
    const config: AxiosRequestConfig = {
      headers,
      responseType,
    };

    try {
      const response = await this.client.post<T>(resource, data, config);
      return response.data;
    } catch (error) {
      console.error('Error in POST request: ', error);
      throw error;
    }
  }

  public async put<T>(
    resource: string,
    data: any,
    headers: Record<string, string> = {},
    responseType: 'json' | 'blob' = 'json'
  ): Promise<T> {
    const config: AxiosRequestConfig = {
      headers,
      responseType,
    };

    try {
      const response = await this.client.put<T>(resource, data, config);
      return response.data;
    } catch (error) {
      console.error('Error in PUT request: ', error);
      throw error;
    }
  }

  public async delete<T>(
    resource: string,
    headers: Record<string, string> = {},
    responseType: 'json' | 'blob' = 'json'
  ): Promise<T> {
    const config: AxiosRequestConfig = {
      headers,
      responseType,
    };

    try {
      const response = await this.client.delete<T>(resource, config);
      return response.data;
    } catch (error) {
      console.error('Error in DELETE request: ', error);
      throw error;
    }
  }
}
