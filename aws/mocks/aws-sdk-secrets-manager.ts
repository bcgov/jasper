import { vi } from "vitest";

const mockSend = vi.fn();

export class SecretsManagerClient {
  send = mockSend;
}

export class GetSecretValueCommand {
  constructor(public input: unknown) {}
}

export class UpdateSecretCommand {
  constructor(public input: unknown) {}
}

export { mockSend };
