import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  DescribeServicesCommand,
  ListServicesCommand,
  mockSend,
  UpdateServiceCommand,
} from "../../mocks/aws-sdk-ecs";
import { ECSService } from "../../services/ecsService"; // Adjust path based on your project

vi.mock("@aws-sdk/client-ecs", () => import("../../mocks/aws-sdk-ecs"));

describe("ECSService", () => {
  let ecsService: ECSService;

  beforeEach(() => {
    ecsService = new ECSService("test-cluster");
    mockSend.mockClear();
  });

  it("should fetch ECS services successfully", async () => {
    const serviceArns = [
      "arn:aws:ecs:region:account:service/service1",
      "arn:aws:ecs:region:account:service/service2",
    ];
    const mockListServicesResponse = { serviceArns };
    const mockDescribeServicesResponse = {
      services: [
        { serviceName: "service1", status: "ACTIVE" },
        { serviceName: "service2", status: "ACTIVE" },
      ],
    };

    mockSend
      .mockResolvedValueOnce(mockListServicesResponse)
      .mockResolvedValueOnce(mockDescribeServicesResponse);

    const services = await ecsService.getECSServices();

    expect(services).toEqual(mockDescribeServicesResponse.services);
    expect(mockSend).toHaveBeenCalledTimes(2);
    expect(mockSend).toHaveBeenCalledWith(expect.any(ListServicesCommand));
    expect(mockSend).toHaveBeenCalledWith(expect.any(DescribeServicesCommand));
  });

  it("should throw error if no services found in cluster", async () => {
    const mockListServicesResponse = { serviceArns: [] };

    mockSend.mockResolvedValueOnce(mockListServicesResponse);

    try {
      await ecsService.getECSServices();
    } catch (error) {
      expect(error.message).toBe(
        "Error occured when listing the services from test-cluster Cluster"
      );
    }
  });

  it("should restart services that are active", async () => {
    const activeServices = [
      { serviceName: "service1", status: "ACTIVE" },
      { serviceName: "service2", status: "ACTIVE" },
    ];

    const mockUpdateResponse = {
      service: "service1",
      taskDefinition: "new-task-def",
    };

    mockSend.mockResolvedValueOnce(mockUpdateResponse);

    await ecsService.restartServices(activeServices);

    expect(mockSend).toHaveBeenCalledTimes(2);
    expect(mockSend).toHaveBeenCalledWith(expect.any(UpdateServiceCommand));
  });

  it("should log when trying to restart inactive services", async () => {
    const inactiveServices = [{ serviceName: "service3", status: "INACTIVE" }];

    const consoleLogSpy = vi.spyOn(console, "log");

    await ecsService.restartServices(inactiveServices);

    expect(consoleLogSpy).toHaveBeenCalledWith(
      "Service service3 is not active, skipping restart."
    );
    consoleLogSpy.mockRestore();
  });
});
