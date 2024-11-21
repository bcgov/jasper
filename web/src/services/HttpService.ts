import Vue from 'vue';

export class HttpService {
  private handleResponse(response): any {
    if (!response.ok) {
      console.error('Error response:', response);
      throw new Error(`API error: ${response.statusText}`);
    }
    return response.body;
  }

  private isHttpError(error: unknown): error is { status: number } {
    return (
      typeof error === 'object' &&
      error !== null &&
      'status' in error &&
      typeof (error as any).status === 'number'
    );
  }

  public async get<T>(resource: string, queryParams: any = null): Promise<T> {
    try {
      const url = new URL(resource);
      Object.entries(queryParams).forEach(([key, value]) =>
        url.searchParams.append(key, value as string)
      );

      const response = await fetch(url);
      return this.handleResponse(response);
    } catch (error) {
      if (this.isHttpError(error)) {
        if (error.status === 401) {
          return this.get(resource);
        }
      }
      throw error;
    }
  }

  public async post<T>(resource: string, data: any): Promise<T> {
    try {
      const response = await Vue.http.post(resource, data);
      return this.handleResponse(response);
    } catch (error) {
      if (this.isHttpError(error)) {
        if (error.status === 401) {
          return this.post(resource, data);
        }
      }
      throw error;
    }
  }
}
