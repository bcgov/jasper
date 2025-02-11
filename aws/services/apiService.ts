import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import qs from "qs";
import { HttpService, IHttpService } from "./httpService";
import SecretsManagerService from "./secretsManagerService";

// These are the list of Headers imported from SCV
// Only include headers from the original request when present.
const allowedHeaders = new Set([
  "applicationCd",
  "correlationId",
  "deviceNm",
  "domainNm",
  "domainUserGuid",
  "domainUserId",
  "guid",
  "ipAddressTxt",
  "reloadPassword",
  "requestAgencyIdentifierId",
  "requestPartId",
  "temporaryAccessGuid",
]);

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

  private sanitizeHeaders(
    headers: Record<string, string | undefined>
  ): Record<string, string> {
    const filteredHeaders: Record<string, string> = {};

    for (const [key, value] of Object.entries(headers || {})) {
      if (allowedHeaders.has(key)) {
        filteredHeaders[key] = value as string;
      }
    }

    return filteredHeaders;
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
      const headers = this.sanitizeHeaders(event.headers);

      const url = `${event.path}?${queryString}`;

      console.log(`Sending ${method} request to ${url}`);
      console.log(`Headers: ${headers}`);
      console.log(`Body: ${body}`);

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
