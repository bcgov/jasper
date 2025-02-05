import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import { HttpService, IHttpService } from "./httpService";
import SecretsManagerService from "./secretsManagerService";

export class ApiService {
  protected httpService: IHttpService;
  protected smService: SecretsManagerService;

  constructor(private credentialsSecret: string) {
    this.smService = new SecretsManagerService();
    this.httpService = new HttpService();
  }

  private async initialize(): Promise<void> {
    const credentialsSecret = await this.smService.getSecret(
      this.credentialsSecret
    );
    const mtlsSecret = await this.smService.getSecret(
      process.env.MTLS_SECRET_NAME!
    );

    await this.httpService.init(credentialsSecret, mtlsSecret);
    console.log("httpService initialized...");
  }

  public async handleRequest(
    event: APIGatewayEvent
  ): Promise<APIGatewayProxyResult> {
    try {
      await this.initialize();

      const method = event.httpMethod.toUpperCase();
      const queryParams = event.queryStringParameters || {};
      const body = event.body ? JSON.parse(event.body) : {};

      let data;

      console.log(`Sending ${method} request to ${event.path}`);

      switch (method) {
        case "GET":
          data = await this.httpService.get(event.path, queryParams);
          break;
        case "POST":
          data = await this.httpService.post(event.path, queryParams);
          break;
        case "PUT":
          data = await this.httpService.put(event.path, body);
          break;
        default:
          return {
            statusCode: 405,
            body: JSON.stringify({ message: `Method ${method} not allowed` }),
          };
      }

      return {
        statusCode: 200,
        body: JSON.stringify(data),
      };
    } catch (error) {
      console.error("Error:", error);

      return {
        statusCode: 500,
        body: JSON.stringify({ message: "Internal Server Error" }),
      };
    }
  }
}
