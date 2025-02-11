import { vi } from "vitest";

const axiosMock = {
  create: vi.fn(() => axiosMock),
  get: vi.fn(),
  post: vi.fn(),
  put: vi.fn(),
  isAxiosError: vi.fn(),
};

export default axiosMock;
