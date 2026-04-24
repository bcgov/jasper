export interface ApiResponse<T> {
  payload: T;
  succeeded: boolean;
  errors: string[];
}

export interface ValidatorErrorResponse {
  message: string;
  errors: string[];
}

export const isValidatorErrorResponse = (
  payload: unknown
): payload is ValidatorErrorResponse => {
  return (
    typeof payload === 'object' &&
    payload !== null &&
    'message' in payload &&
    typeof (payload as ValidatorErrorResponse).message === 'string' &&
    'errors' in payload &&
    Array.isArray((payload as ValidatorErrorResponse).errors)
  );
};

export class CustomAPIError<T> extends Error {
  public originalError: T;
  constructor(message, originalError) {
    super(message);
    this.name = 'CustomAPIError';
    this.originalError = originalError; // Store the original Axios error
  }
}
