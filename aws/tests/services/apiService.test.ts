import { APIGatewayEvent } from "aws-lambda";
import { beforeEach, describe, expect, it, MockedObject, vi } from "vitest";
import { ApiService } from "../../services/apiService";
import { HttpService } from "../../services/httpService";
import { SecretsManagerService } from "../../services/secretsManagerService";

vi.mock("../../services/httpService");
vi.mock("../../services/secretsManagerService");
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

    apiService = new ApiService("test-secret");
    (
      apiService as unknown as { httpService: MockedObject<HttpService> }
    ).httpService = mockHttpService;
    (
      apiService as unknown as {
        smService: MockedObject<SecretsManagerService>;
      }
    ).smService = mockSecretsManagerService;
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

  it("should handle a GET binary request", async () => {
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
    expect(response).toEqual({
      statusCode: 200,
      body: Buffer.from(
        new Uint8Array("get response" as unknown as ArrayBuffer)
      ).toString("base64"),
      isBase64Encoded: true,
      headers: {},
    });
  });
});
