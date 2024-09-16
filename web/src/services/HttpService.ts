import Vue from 'vue';

export class HttpService {
  private handleResponse(response): any {
    if (!response.ok) {
      console.error('Error response:', response);
      throw new Error(`API error: ${response.statusText}`);
    }
    return response.body;
  }

  public async get<T>(resource: string, queryParams = null): Promise<T> {
    try {
      const response = await Vue.http.get(resource, { params: queryParams });
      return this.handleResponse(response);
    } catch (error: any) {
      if (error.status === 401) {
        return this.get(resource);
      }
      throw error;
    }
  }

  public async post<T>(resource: string, data: any): Promise<T> {
    try {
      const response = await Vue.http.post(resource, data);
      return this.handleResponse(response);
    } catch (error: any) {
      if (error.status === 401) {
        return this.post(resource, data);
      }
      throw error;
    }
  }
}
