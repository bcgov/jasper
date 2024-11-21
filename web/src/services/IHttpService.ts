export interface IHttpService {
  get<T>(resource: string, queryParams?: Record<string, any>): Promise<T>;
  post<T>(
    resource: string,
    data: any,
    headers: Record<string, string>,
    responseType: 'json' | 'blob'
  ): Promise<T>;
}
