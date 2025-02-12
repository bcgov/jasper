import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import { sanitizeHeaders, sanitizeQueryStringParams } from "../util";
import { HttpService, IHttpService } from "./httpService";
import { SecretsManagerService } from "./secretsManagerService";

export class ApiService {
  protected httpService: IHttpService;
  protected smService: SecretsManagerService;

  constructor(private credentialsSecret: string) {
    this.smService = new SecretsManagerService();
    this.httpService = new HttpService();
  }

  public async initialize(): Promise<void> {
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
      console.log(event);

      const method = event.httpMethod.toUpperCase();
      const body = event.body ? JSON.parse(event.body) : {};
      const queryString = sanitizeQueryStringParams(
        event.queryStringParameters || {}
      );
      const headers = sanitizeHeaders(event.headers);

      const url = `${event.path}?${queryString}`;

      console.log(`Sending ${method} request to ${url}`);
      console.log(`Headers: ${JSON.stringify(headers, null, 2)}`);
      console.log(`Body: ${JSON.stringify(body, null, 2)}`);

      let data;

      switch (method) {
        case "GET":
          data = await this.httpService.get(url, headers);
          break;
        case "POST":
          data = await this.httpService.post(url, body, headers);
          break;
        case "PUT":
          data = await this.httpService.put(url, body, headers);
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
