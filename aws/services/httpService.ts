import axios, { AxiosInstance, AxiosResponse } from "axios";

export interface IHttpService {
  init(baseURL: string, username: string, password: string): Promise<void>;
  //get<T>(url: string, params?: Record<string, unknown>): Promise<T>;
  get<T>(url: string, username, password): Promise<T>;
  post<T>(url: string, data?: Record<string, unknown>): Promise<T>;
}

export class HttpService implements IHttpService {
  private axios: AxiosInstance;

  async init(
    baseUrl: string,
    username: string,
    password: string
  ): Promise<void> {
    this.axios = axios.create({
      url: baseUrl,
      timeout: 5000,
      auth: { username, password },
    });

    // This is where the PEM file will be pulled and attached to every axios request
  }

  //async get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
  async get<T>(url: string, username, password): Promise<T> {
    try {
      const testUrl = `${this.axios.defaults.url}${url}`;
      console.log(testUrl);
      const up = `${username}:${password}`;
      const response = await axios.get(testUrl, {
        headers: {
          Authorization: `Basic ${btoa(up)}`,
        },
      });

      console.log(response.data);
      return response.data;
    } catch (error) {
      this.handleError(error);
    }

    // try {
    //   console.log(this.axios.defaults.url);
    //   console.log(url);

    //   const response: AxiosResponse<T> = await this.axios.get(url, {
    //     params,
    //   });
    //   return response.data;
    // } catch (error) {
    //   this.handleError(error);
    // }
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
