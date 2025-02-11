import { vi } from "vitest";

const mockSend = vi.fn();

export const ECSClient = vi.fn().mockImplementation(() => ({
  send: mockSend,
}));

export const ListServicesCommand = vi.fn();
export const DescribeServicesCommand = vi.fn();
export const UpdateServiceCommand = vi.fn();

export { mockSend };
