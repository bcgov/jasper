export interface ApiResponse<T> {
  payload: T;
  succeeded: boolean;
  errors: string[];
}

export class CustomAPIError<T> extends Error {
  public originalError: T;
  constructor(message, originalError) {
    super(message);
    this.name = 'CustomAPIError';
    this.originalError = originalError; // Store the original Axios error
  }
}
