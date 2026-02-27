import { vi } from "vitest";

const mockSend = vi.fn();

export class ECSClient {
  send = mockSend;
}

export class ListServicesCommand {
  constructor(public input: unknown) {}
}

export class DescribeServicesCommand {
  constructor(public input: unknown) {}
}

export class UpdateServiceCommand {
  constructor(public input: unknown) {}
}

export { mockSend };
