import { Context } from "aws-lambda";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { handler } from "../../lambdas/proxy/get-assigned-cases-request/index";
import {
  Case,
  GetAssignedCasesRequest,
  GetAssignedCasesResponse,
} from "../../types/get-assigned-cases";

const mockInitialize = vi.fn();
const mockGetAssignedCases = vi.fn();

vi.mock("../../services/assignedCasesService", () => ({
  AssignedCasesService: class AssignedCasesService {
    initialize = mockInitialize;
    getAssignedCases = mockGetAssignedCases;
  },
}));

describe("Get Assigned Cases Request Lambda Handler", () => {
  let mockContext: Partial<Context>;
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

    // Reset mocks to default behavior
    mockInitialize.mockResolvedValue(undefined);
    mockGetAssignedCases.mockResolvedValue({
      success: true,
      data: [],
      message: "Default response",
    });

    mockContext = {
      invokedFunctionArn: "arn:aws:lambda:us-west-2:123456789012:function:test",
      awsRequestId: "test-request-id",
    };
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

    mockGetAssignedCases.mockResolvedValue(mockResponse);

    const result = await handler(
      mockRequest,
      mockContext as Context,
      mockCallback,
    );

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockGetAssignedCases).toHaveBeenCalledWith(mockRequest);
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

    mockGetAssignedCases.mockResolvedValue(mockResponse);

    const result = await handler(
      mockRequest,
      mockContext as Context,
      mockCallback,
    );

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockGetAssignedCases).toHaveBeenCalledWith(mockRequest);
    expect(result).toEqual(mockResponse);
    const { success, data } = result as GetAssignedCasesResponse;
    expect(success).toBe(true);
    expect(data).toHaveLength(0);
  });

  it("should handle service initialization failure", async () => {
    const mockRequest: GetAssignedCasesRequest = {
      reasons: "Trial",
    };

    mockInitialize.mockRejectedValue(new Error("Failed to initialize service"));

    await expect(
      handler(mockRequest, mockContext as Context, mockCallback),
    ).rejects.toThrow("Failed to initialize service");

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockGetAssignedCases).not.toHaveBeenCalled();
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

    mockGetAssignedCases.mockResolvedValue(mockErrorResponse);

    const result = await handler(
      mockRequest,
      mockContext as Context,
      mockCallback,
    );

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockGetAssignedCases).toHaveBeenCalledWith(mockRequest);
    expect(result).toEqual(mockErrorResponse);
    const { success, data } = result as GetAssignedCasesResponse;
    expect(success).toBe(false);
    expect(data).toHaveLength(0);
  });

  it("should handle service throwing an exception", async () => {
    const mockRequest: GetAssignedCasesRequest = {
      reasons: "Trial",
    };

    mockGetAssignedCases.mockRejectedValue(new Error("Network error"));

    await expect(
      handler(mockRequest, mockContext as Context, mockCallback),
    ).rejects.toThrow("Network error");

    expect(mockInitialize).toHaveBeenCalledTimes(1);
    expect(mockGetAssignedCases).toHaveBeenCalledWith(mockRequest);
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

    mockGetAssignedCases.mockResolvedValue(mockResponse);

    await handler(mockRequest, mockContext as Context, mockCallback);

    expect(mockGetAssignedCases).toHaveBeenCalledWith({
      reasons: "Trial,Hearing,Motion",
      restrictions: "Criminal,Civil",
    });
  });
});
