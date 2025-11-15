import { Context } from "aws-lambda";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { handler } from "../../lambdas/proxy/get-assigned-cases-request/index";
import { AssignedCasesService } from "../../services/assignedCasesService";
import {
  Case,
  GetAssignedCasesRequest,
  GetAssignedCasesResponse,
} from "../../types/get-assigned-cases";

vi.mock("../../services/assignedCasesService");

describe("Get Assigned Cases Request Lambda Handler", () => {
  let mockContext: Partial<Context>;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  let mockAssignedCasesService: any;
  const mockCallback = vi.fn();

  const mockCases: Case[] = [
    {
      appearanceId: "12345",
      physicalFileId: "PF-001",
      courtFileNumber: "CF-2024-001",
      courtClass: "Criminal",
      courtLevel: "Supreme",
      appearanceDate: "2024-11-15",
      styleOfCause: "R. v. Smith",
      reason: "Trial",
      dueDate: "2024-12-01",
      judgeId: 1,
      partId: "P-001",
      profPartId: "PP-001",
      participants: [
        { fullName: "John Smith", role: "Accused" },
        { fullName: "Jane Prosecutor", role: "Crown" },
      ],
    },
    {
      appearanceId: "67890",
      physicalFileId: "PF-002",
      courtFileNumber: "CF-2024-002",
      courtClass: "Civil",
      courtLevel: "Provincial",
      appearanceDate: "2024-11-16",
      styleOfCause: "Jones v. Brown",
      reason: "Hearing",
      dueDate: "2024-12-05",
      judgeId: 2,
      partId: "P-002",
      profPartId: "PP-002",
      participants: [
        { fullName: "Alice Jones", role: "Plaintiff" },
        { fullName: "Bob Brown", role: "Defendant" },
      ],
    },
  ];

  beforeEach(() => {
    vi.clearAllMocks();

    mockContext = {
      invokedFunctionArn: "arn:aws:lambda:us-west-2:123456789012:function:test",
      awsRequestId: "test-request-id",
    };

    mockAssignedCasesService = {
      initialize: vi.fn().mockResolvedValue(undefined),
      getAssignedCases: vi.fn(),
    };

    vi.mocked(AssignedCasesService).mockImplementation(
      () => mockAssignedCasesService
    );
  });

  it("should successfully retrieve assigned cases", async () => {
    const mockRequest: GetAssignedCasesRequest = {
      reasons: "DEC,ACT",
      restrictions: "",
    };

    const mockResponse: GetAssignedCasesResponse = {
      success: true,
      data: mockCases,
      message: `Retrieved ${mockCases.length} scheduled cases`,
    };

    mockAssignedCasesService.getAssignedCases.mockResolvedValue(mockResponse);

    const result = await handler(
      mockRequest,
      mockContext as Context,
      mockCallback
    );

    expect(mockAssignedCasesService.initialize).toHaveBeenCalledTimes(1);
    expect(mockAssignedCasesService.getAssignedCases).toHaveBeenCalledWith(
      mockRequest
    );
    expect(result).toEqual(mockResponse);

    const { success, data } = result as GetAssignedCasesResponse;
    expect(success).toBe(true);
    expect(data).toHaveLength(2);
  });

  it("should handle empty request parameters", async () => {
    const mockRequest: GetAssignedCasesRequest = {};

    const mockResponse: GetAssignedCasesResponse = {
      success: true,
      data: [],
      message: "Retrieved 0 scheduled cases",
    };

    mockAssignedCasesService.getAssignedCases.mockResolvedValue(mockResponse);

    const result = await handler(
      mockRequest,
      mockContext as Context,
      mockCallback
    );

    expect(mockAssignedCasesService.initialize).toHaveBeenCalledTimes(1);
    expect(mockAssignedCasesService.getAssignedCases).toHaveBeenCalledWith(
      mockRequest
    );
    expect(result).toEqual(mockResponse);
    const { success, data } = result as GetAssignedCasesResponse;
    expect(success).toBe(true);
    expect(data).toHaveLength(0);
  });

  it("should handle service initialization failure", async () => {
    const mockRequest: GetAssignedCasesRequest = {
      reasons: "Trial",
    };

    mockAssignedCasesService.initialize.mockRejectedValue(
      new Error("Failed to initialize service")
    );

    await expect(
      handler(mockRequest, mockContext as Context, mockCallback)
    ).rejects.toThrow("Failed to initialize service");

    expect(mockAssignedCasesService.initialize).toHaveBeenCalledTimes(1);
    expect(mockAssignedCasesService.getAssignedCases).not.toHaveBeenCalled();
  });

  it("should handle API failure gracefully", async () => {
    const mockRequest: GetAssignedCasesRequest = {
      reasons: "Trial",
      restrictions: "Criminal",
    };

    const mockErrorResponse: GetAssignedCasesResponse = {
      success: false,
      data: [],
      message: "Failed to retrieve scheduled cases",
      error: "Connection timeout",
    };

    mockAssignedCasesService.getAssignedCases.mockResolvedValue(
      mockErrorResponse
    );

    const result = await handler(
      mockRequest,
      mockContext as Context,
      mockCallback
    );

    expect(mockAssignedCasesService.initialize).toHaveBeenCalledTimes(1);
    expect(mockAssignedCasesService.getAssignedCases).toHaveBeenCalledWith(
      mockRequest
    );
    expect(result).toEqual(mockErrorResponse);
    const { success, data } = result as GetAssignedCasesResponse;
    expect(success).toBe(false);
    expect(data).toHaveLength(0);
  });

  it("should handle service throwing an exception", async () => {
    const mockRequest: GetAssignedCasesRequest = {
      reasons: "Trial",
    };

    mockAssignedCasesService.getAssignedCases.mockRejectedValue(
      new Error("Network error")
    );

    await expect(
      handler(mockRequest, mockContext as Context, mockCallback)
    ).rejects.toThrow("Network error");

    expect(mockAssignedCasesService.initialize).toHaveBeenCalledTimes(1);
    expect(mockAssignedCasesService.getAssignedCases).toHaveBeenCalledWith(
      mockRequest
    );
  });

  it("should pass through all request parameters correctly", async () => {
    const mockRequest: GetAssignedCasesRequest = {
      reasons: "Trial,Hearing,Motion",
      restrictions: "Criminal,Civil",
    };

    const mockResponse: GetAssignedCasesResponse = {
      success: true,
      data: mockCases,
      message: `Retrieved ${mockCases.length} scheduled cases`,
    };

    mockAssignedCasesService.getAssignedCases.mockResolvedValue(mockResponse);

    await handler(mockRequest, mockContext as Context, mockCallback);

    expect(mockAssignedCasesService.getAssignedCases).toHaveBeenCalledWith({
      reasons: "Trial,Hearing,Motion",
      restrictions: "Criminal,Civil",
    });
  });
});
