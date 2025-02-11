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
    mockHttpService.get = vi.fn().mockResolvedValue({ data: "get response" });
    mockHttpService.post = vi.fn().mockResolvedValue({ data: "post response" });
    mockHttpService.put = vi.fn().mockResolvedValue({ data: "put response" });
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
      Authorization: "Bearer token",
    });
    expect(response).toEqual({
      statusCode: 200,
      body: JSON.stringify({ data: "get response" }),
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
      { "Content-Type": "application/json" }
    );
    expect(response).toEqual({
      statusCode: 200,
      body: JSON.stringify({ data: "post response" }),
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
});
