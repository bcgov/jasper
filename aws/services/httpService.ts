import axios, { AxiosInstance, AxiosResponse } from "axios";
import https from "https";

export interface IHttpService {
  init(credentialsSecret: string, mtlsSecret: string): Promise<void>;
  get<T>(url: string, params?: Record<string, unknown>): Promise<T>;
  post<T>(url: string, data?: Record<string, unknown>): Promise<T>;
  put<T>(url: string, data?: Record<string, unknown>): Promise<T>;
}

export class HttpService implements IHttpService {
  private axios: AxiosInstance;

  async init(credentialsSecret: string, mtlsSecret: string): Promise<void> {
    const { baseUrl, username, password } = JSON.parse(credentialsSecret);
    const { cert, key } = JSON.parse(mtlsSecret);

    console.log(`Base URL: ${baseUrl}`);

    const httpsAgent = new https.Agent({
      cert,
      key,
      rejectUnauthorized: true,
    });

    this.axios = axios.create({
      baseURL: baseUrl,
      auth: {
        username,
        password,
      },
      httpsAgent,
      headers: {
        "Content-Type": "application/json",
      },
    });
  }

  async get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.axios.get(url, {
        params,
      });
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async post<T>(url: string, data?: Record<string, unknown>): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.axios.post(url, data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  async put<T>(url: string, data?: Record<string, unknown>): Promise<T> {
    try {
      const response: AxiosResponse<T> = await this.axios.put(url, data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }
  }

  private handleError(error: unknown): never {
    if (axios.isAxiosError(error)) {
      console.error("Axios error:", error.message);
      throw new Error(
        `HTTP Error: ${error.response?.status || "Unknown status"}`
      );
    } else {
      console.error("Unexpected error:", error);
      throw new Error("Unexpected error occurred");
    }
  }
}

export default HttpService;
