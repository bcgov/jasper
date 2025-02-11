import { vi } from "vitest";

const mockSend = vi.fn();

export const SecretsManagerClient = vi.fn().mockImplementation(() => ({
  send: mockSend,
}));

export const GetSecretValueCommand = vi.fn();
export const UpdateSecretCommand = vi.fn();

export { mockSend };
