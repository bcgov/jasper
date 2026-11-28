export const SESSION_STATES = {
  INITIALIZING: 'initializing',
  AUTHORIZED: 'authorized',
  ACCESS_REQUIRED: 'access-required',
  FAILED: 'failed',
} as const;

export type SessionState = (typeof SESSION_STATES)[keyof typeof SESSION_STATES];
