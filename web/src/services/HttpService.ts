import axios, { AxiosInstance, AxiosRequestConfig } from 'axios';
import { IHttpService } from './IHttpService';

export class HttpService implements IHttpService {
  private axiosInstance: AxiosInstance;

  constructor(baseURL: string) {
    this.axiosInstance = axios.create({
      baseURL,
      timeout: 10000,
      headers: {
        'Content-Type': 'application/json',
      },
    });
  }

  public async get<T>(
    resource: string,
    queryParams: Record<string, any> = {}
  ): Promise<T> {
    try {
      const response = await this.axiosInstance.get<T>(resource, {
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
      const response = await this.axiosInstance.post<T>(resource, data, config);
      return response.data;
    } catch (error) {
      console.error('Error in POST request: ', error);
      throw error;
    }
  }
}
