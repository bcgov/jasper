import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import qs from "qs";
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

  private sanitizeQueryStringParams(params: Record<string, unknown>) {
    Object.keys(params).forEach((key) => {
      const value = params[key];

      // Check if the value JSON array
      if (
        typeof value === "string" &&
        value.startsWith("[") &&
        value.endsWith("]")
      ) {
        try {
          const parsedValue = JSON.parse(value);

          if (Array.isArray(parsedValue)) {
            params[key] = JSON.stringify(parsedValue);
          }
        } catch (error) {
          console.warn(`Failed to parse ${key}: ${value}`, error);
        }
      }
    });

    const queryString = qs.stringify(params, { encode: true });

    console.log(`Sanitized encoded qs: ${queryString}`);

    return queryString;
  }

  public async handleRequest(
    event: APIGatewayEvent
  ): Promise<APIGatewayProxyResult> {
    try {
      await this.initialize();

      console.log(event);

      const method = event.httpMethod.toUpperCase();
      const body = event.body ? JSON.parse(event.body) : {};

      const queryString = this.sanitizeQueryStringParams(
        event.queryStringParameters || {}
      );

      const url = `${event.path}?${queryString}`;

      console.log(`Sending ${method} request to ${url}`);

      let data;

      switch (method) {
        case "GET":
          data = await this.httpService.get(url);
          break;
        case "POST":
          data = await this.httpService.post(url, body);
          break;
        case "PUT":
          data = await this.httpService.put(url, body);
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
