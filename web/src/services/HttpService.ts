import { useSnackbarStore } from '@/stores/SnackbarStore';
import { CustomAxiosConfig } from '@/types';
import { CustomAPIError } from '@/types/ApiResponse';
import axios, {
  AxiosError,
  AxiosInstance,
  InternalAxiosRequestConfig,
} from 'axios';
import redirectHandlerService from './RedirectHandlerService';

export interface IHttpService {
  get<T>(
    resource: string,
    queryParams?: Record<string, any>,
    config?: CustomAxiosConfig
  ): Promise<T>;
  post<T>(
    resource: string,
    data: any,
    headers?: Record<string, string>,
    responseType?: 'json' | 'blob',
    config?: CustomAxiosConfig
  ): Promise<T>;
  put<T>(
    resource: string,
    data: any,
    headers?: Record<string, string>,
    responseType?: 'json' | 'blob',
    config?: CustomAxiosConfig
  ): Promise<T>;
  delete<T>(
    resource: string,
    headers?: Record<string, string>,
    responseType?: 'json' | 'blob',
    config?: CustomAxiosConfig
  ): Promise<T>;
}

export class HttpService implements IHttpService {
  readonly client: AxiosInstance;
  snackBarStore = useSnackbarStore();

  constructor(baseURL: string) {
    this.client = axios.create({
      baseURL,
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
      (error) => this.handleError(error)
    );
  }

  private handleAuthSuccess(
    config: InternalAxiosRequestConfig
  ): InternalAxiosRequestConfig {
    return config;
  }

  private handleError(error: any) {
    console.error(error);

    if (error.config?.skipErrorHandler) {
      // Component handles the error
      return Promise.reject(
        new CustomAPIError<AxiosError>(error.message, error)
      );
    }

    // todo: check for a 403 and handle it
    if (error.response && error.response.status === 401) {
      redirectHandlerService.handleUnauthorized(window.location.href);
    } else if (error.response && error.response.status === 409) {
      window.location.replace(
        `${import.meta.env.BASE_URL}api/auth/logout?redirectUri=/`
      );
    } else {
      // The user should be notified about unhandled server exceptions.
      this.snackBarStore.showSnackbar(
        'Something went wrong, please contact your Administrator.',
        '#b84157',
        'Error'
      );
    }
    return Promise.reject(new CustomAPIError<AxiosError>(error.message, error));
  }

  public async get<T>(
    resource: string,
    queryParams: Record<string, any> = {},
    config: CustomAxiosConfig = {}
  ): Promise<T> {
    try {
      const response = await this.client.get<T>(resource, {
        params: queryParams,
        ...config,
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
    responseType: 'json' | 'blob' = 'json',
    config: CustomAxiosConfig = {}
  ): Promise<T> {
    try {
      const response = await this.client.post<T>(resource, data, {
        headers,
        responseType,
        ...config,
      });
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
    responseType: 'json' | 'blob' = 'json',
    config: CustomAxiosConfig = {}
  ): Promise<T> {
    try {
      const response = await this.client.put<T>(resource, data, {
        headers,
        responseType,
        ...config,
      });
      return response.data;
    } catch (error) {
      console.error('Error in PUT request: ', error);
      throw error;
    }
  }

  public async delete<T>(
    resource: string,
    headers: Record<string, string> = {},
    responseType: 'json' | 'blob' = 'json',
    config: CustomAxiosConfig = {}
  ): Promise<T> {
    try {
      const response = await this.client.delete<T>(resource, {
        headers,
        responseType,
        ...config,
      });
      return response.data;
    } catch (error) {
      console.error('Error in DELETE request: ', error);
      throw error;
    }
  }
}
