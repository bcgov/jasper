import {
  DescribeServicesCommand,
  ECSClient,
  ListServicesCommand,
  Service,
  UpdateServiceCommand,
} from "@aws-sdk/client-ecs";

export class ECSService {
  private client = new ECSClient();
  private readonly clusterName: string;

  constructor(clusterName: string) {
    this.clusterName = clusterName;
  }

  async getECSServices(): Promise<Service[]> {
    const servicesResponse = await this.client.send(
      new ListServicesCommand({
        cluster: this.clusterName,
      })
    );

    if (
      !servicesResponse ||
      !servicesResponse.serviceArns ||
      servicesResponse.serviceArns.length === 0
    ) {
      throw new Error(
        `Error occured when listing the services from ${this.clusterName} Cluster`
      );
    }

    const describeServicesResponse = await this.client.send(
      new DescribeServicesCommand({
        cluster: this.clusterName,
        services: servicesResponse.serviceArns,
      })
    );

    const services = describeServicesResponse.services;
    if (!services || services.length === 0) {
      throw new Error(
        `Error occured when describing services from ${this.clusterName} Cluster`
      );
    }

    return services;
  }

  async restartServices(services: Service[]): Promise<void> {
    for (const { status, serviceName } of services) {
      if (status === "ACTIVE") {
        const updateServiceCommand = new UpdateServiceCommand({
          cluster: this.clusterName,
          service: serviceName,
          forceNewDeployment: true,
        });

        const response = await this.client.send(updateServiceCommand);
        console.log(`Service ${serviceName} is restarting tasks:`, response);
      } else {
        console.log(`Service ${serviceName} is not active, skipping restart.`);
      }
    }
  }
}

export default ECSService;
