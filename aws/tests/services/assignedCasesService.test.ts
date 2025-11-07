import { beforeEach, describe, expect, it, vi } from "vitest";
import { AssignedCasesService } from "../../services/assignedCasesService";
import HttpService from "../../services/httpService";
import SecretsManagerService from "../../services/secretsManagerService";

vi.mock("../../services/httpService");
vi.mock("../../services/secretsManagerService");

describe("AssignedCasesService", () => {
  let service: AssignedCasesService;
  let mockHttpService: HttpService;
  let mockSecretsManagerService: SecretsManagerService;

  beforeEach(() => {
    vi.clearAllMocks();

    mockHttpService = new HttpService();
    mockSecretsManagerService = new SecretsManagerService();

    service = new AssignedCasesService();
    service["httpService"] = mockHttpService;
    service["smService"] = mockSecretsManagerService;

    process.env.PCSS_SECRET_NAME = "test-pcss-secret";
    process.env.MTLS_SECRET_NAME = "test-mtls-secret";
  });

  describe("initialize", () => {
    it("should initialize successfully with valid secrets", async () => {
      const mockPcssSecret = { username: "test", password: "pass" };
      const mockMtlsSecret = { cert: "cert", key: "key" };

      vi.spyOn(mockSecretsManagerService, "getSecret")
        .mockResolvedValueOnce(JSON.stringify(mockPcssSecret))
        .mockResolvedValueOnce(JSON.stringify(mockMtlsSecret));

      vi.spyOn(mockHttpService, "init").mockResolvedValue();

      await service.initialize();

      expect(mockSecretsManagerService.getSecret).toHaveBeenCalledWith(
        "test-pcss-secret"
      );
      expect(mockSecretsManagerService.getSecret).toHaveBeenCalledWith(
        "test-mtls-secret"
      );
      expect(mockHttpService.init).toHaveBeenCalledWith(
        JSON.stringify(mockPcssSecret),
        JSON.stringify(mockMtlsSecret)
      );
      expect(service["isInitialized"]).toBe(true);
    });

    it("should throw error when PCSS_SECRET_NAME is missing", async () => {
      delete process.env.PCSS_SECRET_NAME;

      await expect(service.initialize()).rejects.toThrow(
        "Missing required environment variables: PCSS_SECRET_NAME or MTLS_SECRET_NAME"
      );
    });

    it("should throw error when MTLS_SECRET_NAME is missing", async () => {
      delete process.env.MTLS_SECRET_NAME;

      await expect(service.initialize()).rejects.toThrow(
        "Missing required environment variables: PCSS_SECRET_NAME or MTLS_SECRET_NAME"
      );
    });

    it("should throw error when both environment variables are missing", async () => {
      delete process.env.PCSS_SECRET_NAME;
      delete process.env.MTLS_SECRET_NAME;

      await expect(service.initialize()).rejects.toThrow(
        "Missing required environment variables: PCSS_SECRET_NAME or MTLS_SECRET_NAME"
      );
    });

    it("should propagate errors from SecretsManager", async () => {
      const mockError = new Error("Secrets Manager failed");
      vi.spyOn(mockSecretsManagerService, "getSecret").mockRejectedValue(
        mockError
      );

      await expect(service.initialize()).rejects.toThrow(
        "Secrets Manager failed"
      );
    });

    it("should propagate errors from httpService.init", async () => {
      vi.spyOn(mockSecretsManagerService, "getSecret")
        .mockResolvedValueOnce(JSON.stringify({ username: "test" }))
        .mockResolvedValueOnce(JSON.stringify({ cert: "cert" }));

      const mockError = new Error("HTTP service init failed");
      vi.spyOn(mockHttpService, "init").mockRejectedValue(mockError);

      await expect(service.initialize()).rejects.toThrow(
        "HTTP service init failed"
      );
    });
  });

  describe("getAssignedCases", () => {
    const mockCases = [
      {
        fileNumber: "12345",
        styleOfCause: "Test v. Case",
        nextAppearance: "2024-11-15",
      },
      {
        fileNumber: "67890",
        styleOfCause: "Another v. Test",
        nextAppearance: "2024-11-16",
      },
    ];

    beforeEach(async () => {
      vi.spyOn(mockSecretsManagerService, "getSecret")
        .mockResolvedValueOnce(JSON.stringify({ username: "test" }))
        .mockResolvedValueOnce(JSON.stringify({ cert: "cert" }));
      vi.spyOn(mockHttpService, "init").mockResolvedValue();
      await service.initialize();
    });

    it("should fetch assigned cases successfully with reasons and restrictions", async () => {
      const mockResponse = {
        data: mockCases,
        status: 200,
        statusText: "OK",
        headers: {},
        config: {},
      };

      vi.spyOn(mockHttpService, "get").mockResolvedValue(mockResponse);

      const request = {
        reasons: "hearing",
        restrictions: "sealed",
      };

      const result = await service.getAssignedCases(request);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        "/api/calendar/judges/upcomingSeizedAssignedCases?reasons=hearing&restrictions=sealed",
        {
          headers: { "Content-Type": "application/json" },
          responseType: "json",
        }
      );

      expect(result).toEqual({
        data: mockCases,
        success: true,
        message: "Retrieved 2 scheduled cases",
      });
    });

    it("should fetch assigned cases with empty reasons and restrictions", async () => {
      const mockResponse = {
        data: mockCases,
        status: 200,
        statusText: "OK",
        headers: {},
        config: {},
      };

      vi.spyOn(mockHttpService, "get").mockResolvedValue(mockResponse);

      const request = {
        reasons: "",
        restrictions: "",
      };

      const result = await service.getAssignedCases(request);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        "/api/calendar/judges/upcomingSeizedAssignedCases?reasons=&restrictions=",
        expect.any(Object)
      );

      expect(result.success).toBe(true);
      expect(result.data).toEqual(mockCases);
    });

    it("should handle empty response array", async () => {
      const mockResponse = {
        data: [],
        status: 200,
        statusText: "OK",
        headers: {},
        config: {},
      };

      vi.spyOn(mockHttpService, "get").mockResolvedValue(mockResponse);

      const request = {
        reasons: "hearing",
        restrictions: "sealed",
      };

      const result = await service.getAssignedCases(request);

      expect(result).toEqual({
        data: [],
        success: true,
        message: "Retrieved 0 scheduled cases",
      });
    });

    it("should throw error when service not initialized", async () => {
      const uninitializedService = new AssignedCasesService();

      const request = {
        reasons: "hearing",
        restrictions: "sealed",
      };

      await expect(
        uninitializedService.getAssignedCases(request)
      ).rejects.toThrow(
        "AssignedCasesService not initialized. Call initialize() first."
      );
    });

    it("should handle HTTP errors and return failure response", async () => {
      const mockError = new Error("Network error");
      vi.spyOn(mockHttpService, "get").mockRejectedValue(mockError);

      const request = {
        reasons: "hearing",
        restrictions: "sealed",
      };

      const result = await service.getAssignedCases(request);

      expect(result).toEqual({
        data: [],
        success: false,
        message: "Network error",
      });
    });

    it("should handle non-Error exceptions", async () => {
      vi.spyOn(mockHttpService, "get").mockRejectedValue("String error");

      const request = {
        reasons: "hearing",
        restrictions: "sealed",
      };

      const result = await service.getAssignedCases(request);

      expect(result).toEqual({
        data: [],
        success: false,
        message: "Failed to retrieve scheduled cases",
      });
    });

    it("should handle undefined reasons and restrictions", async () => {
      const mockResponse = {
        data: mockCases,
        status: 200,
        statusText: "OK",
        headers: {},
        config: {},
      };

      vi.spyOn(mockHttpService, "get").mockResolvedValue(mockResponse);

      const request = {
        reasons: undefined,
        restrictions: undefined,
      };

      const result = await service.getAssignedCases(request);

      expect(mockHttpService.get).toHaveBeenCalledWith(
        "/api/calendar/judges/upcomingSeizedAssignedCases?reasons=&restrictions=",
        expect.any(Object)
      );

      expect(result.success).toBe(true);
      expect(result.data).toEqual(mockCases);
    });
  });
});
