import { APIGatewayEvent } from "aws-lambda";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { handler } from "../../lambdas/proxy/proxy-request/index";

const mockInitialize = vi.fn();
const mockHandleRequest = vi.fn();
const mockApiServiceCtor = vi.fn();

vi.mock("../../services/apiService", () => ({
  ApiService: class ApiService {
    constructor(public secretName: string) {
      mockApiServiceCtor(secretName);
    }
    initialize = mockInitialize;
    handleRequest = mockHandleRequest;
  },
}));

describe("Lambda Handler", () => {
  let mockEvent: Partial<APIGatewayEvent>;

  const darsSecret = "dars-secret";
  const pcssSecret = "pcss-secret";
  const tdSecret = "td-secret";
  const fileServicesClientSecret = "files-services-client-secret";

  beforeEach(() => {
    vi.clearAllMocks();

    // Reset mocks to default behavior
    mockInitialize.mockResolvedValue(undefined);
    mockHandleRequest.mockResolvedValue({
      statusCode: 200,
      body: JSON.stringify({ message: "Success" }),
    });

    process.env.DARS_SECRET_NAME = darsSecret;
    process.env.PCSS_SECRET_NAME = pcssSecret;
    process.env.TD_SECRET_NAME = tdSecret;
    process.env.FILE_SERVICES_CLIENT_SECRET_NAME = fileServicesClientSecret;

    mockEvent = {
      headers: {},
    };
  });

  it("should use DARS secret when x-target-app is DARS", async () => {
    mockEvent.headers!["x-target-app"] = "DARS";

    const response = await handler(mockEvent as APIGatewayEvent);

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockApiServiceCtor).toHaveBeenCalledWith(darsSecret);
    expect(mockHandleRequest).toHaveBeenCalledWith(mockEvent);
    expect(response.statusCode).toBe(200);
  });

  it("should use PCSS secret when x-target-app is PCSS", async () => {
    mockEvent.headers!["x-target-app"] = "PCSS";

    const response = await handler(mockEvent as APIGatewayEvent);

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockApiServiceCtor).toHaveBeenCalledWith(pcssSecret);
    expect(mockHandleRequest).toHaveBeenCalledWith(mockEvent);
    expect(response.statusCode).toBe(200);
  });

  it("should use TD secret when x-target-app is TD", async () => {
    mockEvent.headers!["x-target-app"] = "TD";

    const response = await handler(mockEvent as APIGatewayEvent);

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockApiServiceCtor).toHaveBeenCalledWith(tdSecret);
    expect(mockHandleRequest).toHaveBeenCalledWith(mockEvent);
    expect(response.statusCode).toBe(200);
  });

  it("should use default secret when x-target-app is missing", async () => {
    const response = await handler(mockEvent as APIGatewayEvent);

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockApiServiceCtor).toHaveBeenCalledWith(fileServicesClientSecret);
    expect(mockHandleRequest).toHaveBeenCalledWith(mockEvent);
    expect(response.statusCode).toBe(200);
  });
});
