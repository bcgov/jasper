import { APIGatewayRequestAuthorizerEvent, Context } from "aws-lambda";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { handler } from "../../lambdas/auth/authorizer/index";

vi.mock("../../services/secretsManagerService", () => ({
  default: vi.fn().mockImplementation(() => ({
    getSecret: vi
      .fn()
      .mockResolvedValue(JSON.stringify({ verifyKey: "test-secret-key" })),
  })),
}));

describe("Authorizer Lambda Handler", () => {
  let mockEvent: Partial<APIGatewayRequestAuthorizerEvent>;
  let mockContext: Partial<Context>;

  beforeEach(() => {
    process.env.VERIFY_SECRET_NAME = "test-secret";

    mockEvent = {
      headers: { "x-origin-verify": "test-secret-key" },
      methodArn:
        "arn:aws:execute-api:region:account-id:api-id/stage/GET/resource",
    };

    mockContext = {};
  });

  it("should return an Allow policy when token matches secret", async () => {
    const response = await handler(
      mockEvent as APIGatewayRequestAuthorizerEvent,
      mockContext as Context
    );
    expect(response.policyDocument.Statement[0].Effect).toBe("Allow");
  });

  it("should throw an error when headers are missing", async () => {
    delete mockEvent.headers;
    await expect(
      handler(
        mockEvent as APIGatewayRequestAuthorizerEvent,
        mockContext as Context
      )
    ).rejects.toThrow("Unauthorized");
  });

  it("should throw an error when x-origin-verify header is missing", async () => {
    delete mockEvent.headers!["x-origin-verify"];
    await expect(
      handler(
        mockEvent as APIGatewayRequestAuthorizerEvent,
        mockContext as Context
      )
    ).rejects.toThrow("Unauthorized");
  });

  it("should throw an error when token does not match secret", async () => {
    mockEvent.headers!["x-origin-verify"] = "invalid-key";
    await expect(
      handler(
        mockEvent as APIGatewayRequestAuthorizerEvent,
        mockContext as Context
      )
    ).rejects.toThrow("Unauthorized");
  });
});
