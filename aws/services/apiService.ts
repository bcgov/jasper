import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import { AxiosRequestConfig, AxiosResponse } from "axios";
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

      console.log(`Headers: ${JSON.stringify(headers, null, 2)}`);

      console.log(`Body: ${JSON.stringify(body, null, 2)}`);

      // Determine if request expects a binary response
      const isBinary =
        headers && headers["Accept"]?.startsWith("application/octet-stream");

      const axiosConfig: AxiosRequestConfig = {
        headers: {
          ...headers,
          "Content-Type": isBinary
            ? "application/octet-stream"
            : "application/json",
        },
        responseType: isBinary ? "arraybuffer" : "json",
      };

      let response: AxiosResponse<unknown>;

      switch (method) {
        case "GET":
          response = await this.httpService.get(url, axiosConfig);
          break;
        case "POST":
          response = await this.httpService.post(url, body, axiosConfig);
          break;
        case "PUT":
          response = await this.httpService.put(url, body, axiosConfig);
          break;
        default:
          return {
            statusCode: 405,
            body: JSON.stringify({ message: `Method ${method} not allowed` }),
          };
      }

      console.log("Response:", response);

      // Ensure headers are properly typed
      const responseHeaders = Object.fromEntries(
        Object.entries(response.headers)
          .filter(([, value]) => value !== undefined) // Use `[, value]` to ignore unused key
          .map(([key, value]) => [key, String(value)]) // Ensure all values are strings
      ) as { [header: string]: string | number | boolean };

      return {
        statusCode: 200,
        headers: responseHeaders,
        body: isBinary
          ? Buffer.from(new Uint8Array(response.data as ArrayBuffer)).toString(
              "base64"
            )
          : JSON.stringify(response.data),
        isBase64Encoded: !!isBinary,
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
