import { useSnackbarStore } from '@/stores/SnackbarStore';
import { CustomAxiosConfig } from '@/types';
import {
  CustomAPIError,
  isValidatorErrorResponse,
  ValidatorErrorResponse,
} from '@/types/ApiResponse';
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
        'X-Timezone': Intl.DateTimeFormat().resolvedOptions().timeZone,
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

  private async handleError(error: any) {
    console.error(error);

    if (error.config?.skipErrorHandler) {
      // Component handles the error
      return Promise.reject(
        new CustomAPIError<AxiosError>(error.message, error)
      );
    }

    const status = error.response?.status;

    const validatorError = await this.getValidatorError(error);

    // todo: check for a 403 and handle it
    if (status === 401) {
      redirectHandlerService.handleUnauthorized(window.location.href);
    } else if (status === 409) {
      window.location.replace(
        `${import.meta.env.BASE_URL}api/auth/logout?redirectUri=/`
      );
    } else if (validatorError != null && status === 400) {
      this.snackBarStore.showSnackbar(
        `${validatorError.errors.join('\n')}`,
        '#b84157',
        'Validation Error'
      );
    } else if (status && status !== 403) {
      // The user should be notified about unhandled server exceptions.
      this.snackBarStore.showSnackbar(
        'Something went wrong, please contact your Administrator.',
        '#b84157',
        'Error'
      );
    }

    return Promise.reject(new CustomAPIError<AxiosError>(error.message, error));
  }

  private async getValidatorError(
    error?: AxiosError
  ): Promise<ValidatorErrorResponse | null> {
    if (!error?.response) {
      return null;
    }

    const response = error.response;
    const data = response.data;

    if (typeof Blob !== 'undefined' && data instanceof Blob) {
      const contentType =
        response.headers?.['content-type'] ??
        response.headers?.['Content-Type'];

      if (
        typeof contentType === 'string' &&
        contentType.includes('application/json')
      ) {
        try {
          const text = await data.text();
          return JSON.parse(text);
        } catch {
          return null;
        }
      }

      return null;
    }

    if (typeof data === 'string') {
      try {
        const parsedError = JSON.parse(data);
        if (isValidatorErrorResponse(parsedError)) {
          return parsedError;
        }
      } catch {
        return null;
      }
    }

    return null;
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
      // Handle 204 No Content
      if (response.status === 204) {
        return null as T;
      }
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
