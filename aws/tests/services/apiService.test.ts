import { APIGatewayEvent } from "aws-lambda";
import { beforeEach, describe, expect, it, MockedObject, vi } from "vitest";
import { ApiService } from "../../services/apiService";
import { EFSService } from "../../services/efsService";
import { HttpService } from "../../services/httpService";
import { SecretsManagerService } from "../../services/secretsManagerService";

vi.mock("../../services/httpService");
vi.mock("../../services/secretsManagerService");
vi.mock("../../services/efsService");
vi.mock("../../util", () => ({
  sanitizeHeaders: vi.fn((headers) => headers),
  sanitizeQueryStringParams: vi.fn((params) =>
    new URLSearchParams(params).toString()
  ),
}));

describe("ApiService", () => {
  let apiService: ApiService;
  let mockHttpService: MockedObject<HttpService>;
  let mockSecretsManagerService: MockedObject<SecretsManagerService>;
  let mockEfsService: MockedObject<EFSService>;

  beforeEach(() => {
    mockHttpService = new HttpService() as MockedObject<HttpService>;
    mockHttpService.get = vi
      .fn()
      .mockResolvedValue({ data: "get response", headers: {}, status: 200 });
    mockHttpService.post = vi
      .fn()
      .mockResolvedValue({ data: "post response", headers: {}, status: 200 });
    mockHttpService.put = vi
      .fn()
      .mockResolvedValue({ data: "put response", headers: {}, status: 204 });
    mockHttpService.init = vi.fn();

    mockSecretsManagerService =
      new SecretsManagerService() as MockedObject<SecretsManagerService>;
    mockSecretsManagerService.getSecret = vi
      .fn()
      .mockResolvedValue("mock-secret");

    mockEfsService = new EFSService() as MockedObject<EFSService>;
    mockEfsService.saveFile = vi
      .fn()
      .mockResolvedValue("/mnt/efs/test-file.pdf");

    apiService = new ApiService("test-secret");
    (
      apiService as unknown as { httpService: MockedObject<HttpService> }
    ).httpService = mockHttpService;
    (
      apiService as unknown as {
        smService: MockedObject<SecretsManagerService>;
      }
    ).smService = mockSecretsManagerService;
    (
      apiService as unknown as { efsService: MockedObject<EFSService> }
    ).efsService = mockEfsService;
  });

  it("should initialize the service", async () => {
    await apiService.initialize();
    expect(mockSecretsManagerService.getSecret).toHaveBeenCalledWith(
      "test-secret"
    );
    expect(mockSecretsManagerService.getSecret).toHaveBeenCalledWith(
      process.env.MTLS_SECRET_NAME
    );
    expect(mockHttpService.init).toHaveBeenCalledWith(
      "mock-secret",
      "mock-secret"
    );
  });

  it("should handle a GET request", async () => {
    const event: Partial<APIGatewayEvent> = {
      httpMethod: "GET",
      path: "/test",
      queryStringParameters: { key: "value" },
      headers: { Authorization: "Bearer token" },
      body: null,
    };

    const response = await apiService.handleRequest(event as APIGatewayEvent);

    expect(mockHttpService.get).toHaveBeenCalledWith("/test?key=value", {
      headers: {
        Authorization: "Bearer token",
        "Content-Type": "application/json",
      },
      responseType: "json",
    });
    expect(response).toEqual({
      statusCode: 200,
      body: JSON.stringify("get response"),
      isBase64Encoded: false,
      headers: {},
    });
  });

  it("should handle a POST request", async () => {
    const event: Partial<APIGatewayEvent> = {
      httpMethod: "POST",
      path: "/test",
      queryStringParameters: {},
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: "test" }),
    };

    const response = await apiService.handleRequest(event as APIGatewayEvent);

    expect(mockHttpService.post).toHaveBeenCalledWith(
      "/test?",
      { name: "test" },
      { headers: { "Content-Type": "application/json" }, responseType: "json" }
    );
    expect(response).toEqual({
      statusCode: 200,
      body: JSON.stringify("post response"),
      isBase64Encoded: false,
      headers: {},
    });
  });

  it("should return 405 for unsupported methods", async () => {
    const event: Partial<APIGatewayEvent> = {
      httpMethod: "DELETE",
      path: "/test",
    };
    const response = await apiService.handleRequest(event as APIGatewayEvent);
    expect(response).toEqual({
      statusCode: 405,
      body: JSON.stringify({ message: "Method DELETE not allowed" }),
    });
  });

  it("should return 500 for an internal error", async () => {
    mockHttpService.get.mockRejectedValue(new Error("Test error"));
    const event: Partial<APIGatewayEvent> = {
      httpMethod: "GET",
      path: "/test",
    };
    const response = await apiService.handleRequest(event as APIGatewayEvent);
    expect(response).toEqual({
      statusCode: 500,
      body: JSON.stringify({ message: "Internal Server Error" }),
    });
  });

  it("should handle a GET binary request with small file (< 4.5MB)", async () => {
    const smallBinaryData = new ArrayBuffer(1024 * 1024); // 1MB
    mockHttpService.get = vi.fn().mockResolvedValue({
      data: smallBinaryData,
      headers: { "content-type": "application/pdf" },
      status: 200,
    });

    const event: Partial<APIGatewayEvent> = {
      httpMethod: "GET",
      path: "/test",
      queryStringParameters: { key: "value" },
      headers: { Accept: "application/octet-stream" },
      body: null,
    };

    const response = await apiService.handleRequest(event as APIGatewayEvent);

    expect(mockHttpService.get).toHaveBeenCalledWith("/test?key=value", {
      headers: {
        Accept: "application/octet-stream",
        "Content-Type": "application/octet-stream",
      },
      responseType: "arraybuffer",
    });
    expect(mockEfsService.saveFile).not.toHaveBeenCalled();
    expect(response.statusCode).toBe(200);
    expect(response.isBase64Encoded).toBe(true);
    expect(response.headers).toEqual({ "content-type": "application/pdf" });
  });

  it("should save large binary file to EFS (> 4.5MB)", async () => {
    const largeBinaryData = new ArrayBuffer(5 * 1024 * 1024); // 5MB
    mockHttpService.get = vi.fn().mockResolvedValue({
      data: largeBinaryData,
      headers: { "content-type": "application/pdf" },
      status: 200,
    });

    const event: Partial<APIGatewayEvent> = {
      httpMethod: "GET",
      path: "/test/large-file",
      queryStringParameters: {},
      headers: { Accept: "application/octet-stream" },
      body: null,
    };

    const response = await apiService.handleRequest(event as APIGatewayEvent);

    expect(mockEfsService.saveFile).toHaveBeenCalledWith(expect.any(Buffer));
    expect(response.statusCode).toBe(200);
    expect(response.isBase64Encoded).toBe(false);
    expect(response.headers).toBeDefined();
    expect(response.headers!["X-EFS-File-Path"]).toBe("/mnt/efs/test-file.pdf");
    expect(response.headers!["X-File-Size"]).toBe(String(5 * 1024 * 1024));
    expect(JSON.parse(response.body)).toEqual({
      message: "File saved to EFS due to size limit",
      filePath: "/mnt/efs/test-file.pdf",
      fileSize: 5 * 1024 * 1024,
    });
  });

  it("should handle exactly 4.5MB file as threshold", async () => {
    const thresholdData = new ArrayBuffer(4.5 * 1024 * 1024); // Exactly 4.5MB
    mockHttpService.get = vi.fn().mockResolvedValue({
      data: thresholdData,
      headers: {},
      status: 200,
    });

    const event: Partial<APIGatewayEvent> = {
      httpMethod: "GET",
      path: "/test/threshold",
      headers: { Accept: "application/octet-stream" },
    };

    const response = await apiService.handleRequest(event as APIGatewayEvent);

    // At exactly 4.5MB, should still return base64 (not > threshold)
    expect(mockEfsService.saveFile).not.toHaveBeenCalled();
    expect(response.isBase64Encoded).toBe(true);
  });

  it("should save file to EFS when size is just over 4.5MB", async () => {
    const justOverThreshold = new ArrayBuffer(4.5 * 1024 * 1024 + 1); // 4.5MB + 1 byte
    mockHttpService.get = vi.fn().mockResolvedValue({
      data: justOverThreshold,
      headers: {},
      status: 200,
    });

    const event: Partial<APIGatewayEvent> = {
      httpMethod: "GET",
      path: "/test/just-over",
      headers: { Accept: "application/octet-stream" },
    };

    const response = await apiService.handleRequest(event as APIGatewayEvent);

    expect(mockEfsService.saveFile).toHaveBeenCalled();
    expect(response.headers).toBeDefined();
    expect(response.headers!["X-EFS-File-Path"]).toBe("/mnt/efs/test-file.pdf");
  });
});
