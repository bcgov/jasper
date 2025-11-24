import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from "axios";
import * as https from "https";

export interface IHttpService {
  init(credentialsSecret: string, mtlsSecret: string): Promise<void>;
  get<T>(url: string, config: AxiosRequestConfig): Promise<AxiosResponse<T>>;
  post<T>(
    url: string,
    data: Record<string, unknown>,
    config: AxiosRequestConfig
  ): Promise<AxiosResponse<T>>;
  put<T>(
    url: string,
    data: Record<string, unknown>,
    config: AxiosRequestConfig
  ): Promise<AxiosResponse<T>>;
}

export class HttpService implements IHttpService {
  private axios: AxiosInstance;

  async init(credentialsSecret: string, mtlsSecret: string): Promise<void> {
    const { baseUrl, username, password } = JSON.parse(credentialsSecret);
    const { cert, key } = JSON.parse(mtlsSecret);

    console.log(`Base URL: ${baseUrl}`);

    const httpsAgent = new https.Agent({
      // Replace escaped `\n` with real newlines
      cert: cert.replace(/\\n/g, "\n"),
      key: key.replace(/\\n/g, "\n"),
      rejectUnauthorized: true,
    });

    this.axios = axios.create({
      baseURL: baseUrl,
      auth: {
        username,
        password,
      },
      httpsAgent,
    });
  }

  async get<T>(
    url: string,
    config: AxiosRequestConfig
  ): Promise<AxiosResponse<T>> {
    try {
      const response: AxiosResponse<T> = await this.axios.get(url, config);
      return response;
    } catch (error) {
      this.handleError(error);
    }
  }

  async post<T>(
    url: string,
    data: Record<string, unknown>,
    config: AxiosRequestConfig
  ): Promise<AxiosResponse<T>> {
    try {
      const response: AxiosResponse<T> = await this.axios.post(
        url,
        data,
        config
      );
      return response;
    } catch (error) {
      this.handleError(error);
    }
  }

  async put<T>(
    url: string,
    data: Record<string, unknown>,
    config: AxiosRequestConfig
  ): Promise<AxiosResponse<T>> {
    try {
      const response: AxiosResponse<T> = await this.axios.put(
        url,
        data,
        config
      );
      return response;
    } catch (error) {
      this.handleError(error);
    }
  }

  private handleError(error: unknown): never {
    if (axios.isAxiosError(error)) {
      console.error("Axios error:", error.message);
      throw error;
    } else {
      console.error("Unexpected error:", error);
      throw new Error("Unexpected error occurred");
    }
  }
}

export default HttpService;
