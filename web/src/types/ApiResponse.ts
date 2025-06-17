export interface ApiResponse<T> {
  payload: T;
  succeeded: boolean;
  errors: string[];
}
