import { beforeEach, describe, expect, it, vi } from "vitest";

vi.mock(
  "@aws-sdk/client-secrets-manager",
  () => import("../../mocks/aws-sdk-secrets-manager")
);

import {
  GetSecretValueCommand,
  mockSend,
  UpdateSecretCommand,
} from "../../mocks/aws-sdk-secrets-manager";
import { SecretsManagerService } from "../../services/secretsManagerService";

describe("SecretsManagerService", () => {
  let service: SecretsManagerService;

  beforeEach(() => {
    service = new SecretsManagerService();
    mockSend.mockClear();
  });

  it("should return the secret value when getSecret is called", async () => {
    const secretName = "test-secret";
    const mockResponse = { SecretString: "mock-secret-value" };

    mockSend.mockResolvedValueOnce(mockResponse);

    const result = await service.getSecret(secretName);

    expect(result).toBe("mock-secret-value");
    expect(mockSend).toHaveBeenCalledWith(expect.any(GetSecretValueCommand));
  });

  it("should throw an error if SecretString is missing", async () => {
    const secretName = "test-secret";
    const mockResponse = {};

    mockSend.mockResolvedValueOnce(mockResponse);

    await expect(service.getSecret(secretName)).rejects.toThrow(
      `Secret with ID ${secretName} does not contain SecretString`
    );

    expect(mockSend).toHaveBeenCalledWith(expect.any(GetSecretValueCommand));
  });

  it("should call updateSecret correctly", async () => {
    const secretId = "test-secret";
    const secretString = "new-secret-value";

    mockSend.mockResolvedValueOnce({}); // Mock successful update

    await service.updateSecret(secretId, secretString);

    expect(mockSend).toHaveBeenCalledWith(expect.any(UpdateSecretCommand));
  });
});
