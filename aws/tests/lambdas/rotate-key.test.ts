import { APIGatewayEvent, Context } from "aws-lambda";
import { beforeEach, describe, expect, it, Mock, vi } from "vitest";
import { handler } from "../../lambdas/auth/rotate-key/index";
import SecretsManagerService from "../../services/secretsManagerService";

vi.mock("../../services/ecsService", () => ({
  default: vi.fn().mockImplementation(() => ({
    getECSServices: vi.fn().mockResolvedValue(["service1", "service2"]),
    restartServices: vi.fn().mockResolvedValue(undefined),
  })),
}));

vi.mock("../../services/secretsManagerService", () => ({
  default: vi.fn().mockImplementation(() => ({
    updateSecret: vi.fn().mockResolvedValue(undefined),
  })),
}));

describe("Rotate Key Lambda Handler", () => {
  let mockEvent: Partial<APIGatewayEvent>;
  let mockContext: Partial<Context>;

  beforeEach(() => {
    process.env.VERIFY_SECRET_NAME = "test-secret";
    process.env.CLUSTER_NAME = "test-cluster";

    mockEvent = { headers: {} };
    mockContext = {};
  });

  it("should successfully rotate key and restart ECS services", async () => {
    const response = await handler(
      mockEvent as APIGatewayEvent,
      mockContext as Context
    );
    expect(response.statusCode).toBe(200);
    expect(response.body).toContain("Successfully rotated the key");
  });

  it("should return an error if an exception occurs", async () => {
    (SecretsManagerService as unknown as Mock).mockImplementation(() => ({
      updateSecret: vi
        .fn()
        .mockRejectedValue(new Error("Secret update failed")),
    }));

    const response = await handler(
      mockEvent as APIGatewayEvent,
      mockContext as Context
    );
    expect(response.statusCode).toBe(500);
    expect(response.body).toContain("Something went wrong");
  });
});
