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
  const password = process.env.TEST_PASSWORD;
  const cert = "mock-cert";
  const key = "mock-key";

  beforeEach(() => {
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
    axiosMock.get.mockResolvedValue({ data: { message: "success" } });

    await httpService.init(credentialsSecret, mtlsSecret);
    const result = await httpService.get("/test");

    expect(result).toEqual({ message: "success" });
    expect(axiosMock.get).toHaveBeenCalledWith("/test", { headers: undefined });
  });

  it("should perform POST request", async () => {
    axiosMock.post.mockResolvedValue({ data: { id: 1 } });

    await httpService.init(credentialsSecret, mtlsSecret);
    const result = await httpService.post("/test", { name: "example" });

    expect(result).toEqual({ id: 1 });
    expect(axiosMock.post).toHaveBeenCalledWith(
      "/test",
      { name: "example" },
      { headers: undefined }
    );
  });

  it("should perform PUT request", async () => {
    axiosMock.put.mockResolvedValue({ data: { updated: true } });

    await httpService.init(credentialsSecret, mtlsSecret);
    const result = await httpService.put("/test", { name: "updated" });

    expect(result).toEqual({ updated: true });
    expect(axiosMock.put).toHaveBeenCalledWith(
      "/test",
      { name: "updated" },
      { headers: undefined }
    );
  });

  it("should handle errors correctly", async () => {
    axiosMock.get.mockRejectedValue({ response: { status: 404 } });
    axiosMock.isAxiosError.mockReturnValue(true);

    await httpService.init(credentialsSecret, mtlsSecret);
    await expect(httpService.get("/not-found")).rejects.toThrow(
      "HTTP Error: 404"
    );
  });
});
