import { AxiosRequestConfig } from "axios";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { default as axiosMock } from "../../mocks/axios";
import HttpService from "../../services/httpService";

vi.mock("axios", () => import("../../mocks/axios"));

describe("HttpService", () => {
  let httpService: HttpService;
  let credentialsSecret: string;
  let mtlsSecret: string;

  const baseUrl = "https://api.example.com";
  const username = "user";
  const password = "test-password";
  const cert = "mock-cert";
  const key = "mock-key";

  const mockAxiosConfig: AxiosRequestConfig = {
    headers: {
      "Content-Type": "application/json",
    },
  };

  beforeEach(() => {
    vi.clearAllMocks();
    httpService = new HttpService();
    credentialsSecret = JSON.stringify({
      baseUrl,
      username,
      password,
    });
    mtlsSecret = JSON.stringify({
      cert,
      key,
    });
  });

  it("should initialize axios instance with correct configs", async () => {
    const mockInstance = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn(),
    };
    axiosMock.create.mockReturnValue(mockInstance as any);

    await httpService.init(credentialsSecret, mtlsSecret);

    expect(axiosMock.create).toHaveBeenCalledWith(
      expect.objectContaining({
        baseURL: baseUrl,
        auth: {
          username,
          password,
        },
        httpsAgent: expect.anything(),
      })
    );
  });

  it("should perform GET request", async () => {
    const mockInstance = {
      get: vi.fn().mockResolvedValue({ data: { message: "success" } }),
      post: vi.fn(),
      put: vi.fn(),
    };
    axiosMock.create.mockReturnValue(mockInstance as any);

    await httpService.init(credentialsSecret, mtlsSecret);
    const response = await httpService.get("/test", mockAxiosConfig);

    expect(response.data).toEqual({ message: "success" });
    expect(mockInstance.get).toHaveBeenCalledWith("/test", mockAxiosConfig);
  });

  it("should perform POST request", async () => {
    const mockInstance = {
      get: vi.fn(),
      post: vi.fn().mockResolvedValue({ data: { id: 1 } }),
      put: vi.fn(),
    };
    axiosMock.create.mockReturnValue(mockInstance as any);

    await httpService.init(credentialsSecret, mtlsSecret);
    const response = await httpService.post(
      "/test",
      { name: "example" },
      mockAxiosConfig
    );

    expect(response.data).toEqual({ id: 1 });
    expect(mockInstance.post).toHaveBeenCalledWith(
      "/test",
      { name: "example" },
      mockAxiosConfig
    );
  });

  it("should perform PUT request", async () => {
    const mockInstance = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn().mockResolvedValue({ data: { updated: true } }),
    };
    axiosMock.create.mockReturnValue(mockInstance as any);

    await httpService.init(credentialsSecret, mtlsSecret);
    const response = await httpService.put(
      "/test",
      { name: "updated" },
      mockAxiosConfig
    );

    expect(response.data).toEqual({ updated: true });
    expect(mockInstance.put).toHaveBeenCalledWith(
      "/test",
      { name: "updated" },
      mockAxiosConfig
    );
  });

  it("should handle errors correctly", async () => {
    const axiosError = {
      response: { status: 404 },
      isAxiosError: true,
      message: "Request failed with status code 404",
    };
    const mockInstance = {
      get: vi.fn().mockRejectedValue(axiosError),
      post: vi.fn(),
      put: vi.fn(),
    };
    axiosMock.create.mockReturnValue(mockInstance as any);
    axiosMock.isAxiosError.mockReturnValue(true);

    await httpService.init(credentialsSecret, mtlsSecret);
    await expect(
      httpService.get("/not-found", mockAxiosConfig)
    ).rejects.toMatchObject({ response: { status: 404 } });
  });
});
