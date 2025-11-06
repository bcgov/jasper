import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import { AxiosRequestConfig, AxiosResponse } from "axios";
import { sanitizeHeaders, sanitizeQueryStringParams } from "../util";
import { EFSService } from "./efsService";
import { HttpService, IHttpService } from "./httpService";
import { SecretsManagerService } from "./secretsManagerService";

type ResponseHeaders = {
  [header: string]: string | number | boolean;
};

export class ApiService {
  protected httpService: IHttpService;
  protected smService: SecretsManagerService;
  protected efsService: EFSService;

  constructor(private credentialsSecret: string) {
    this.smService = new SecretsManagerService();
    this.httpService = new HttpService();
    this.efsService = new EFSService();
  }

  public async initialize(): Promise<void> {
    const credentialsSecret = await this.smService.getSecret(
      this.credentialsSecret
    );
    const mtlsSecret = await this.smService.getSecret(
      process.env.MTLS_SECRET_NAME!
    );

    await this.httpService.init(credentialsSecret, mtlsSecret);
  }

  private async saveBinaryToEFS(
    response: AxiosResponse<unknown>
  ): Promise<string> {
    const binaryData = Buffer.from(
      new Uint8Array(response.data as ArrayBuffer)
    );

    const filePath = await this.efsService.saveFile(binaryData);
    console.log(`Binary file saved to EFS: ${filePath}`);

    return filePath;
  }

  private async executeHttpRequest(
    method: string,
    url: string,
    body: Record<string, unknown>,
    config: AxiosRequestConfig
  ): Promise<AxiosResponse<unknown>> {
    switch (method) {
      case "GET":
        return await this.httpService.get(url, config);
      case "POST":
        return await this.httpService.post(url, body, config);
      case "PUT":
        return await this.httpService.put(url, body, config);
      default:
        throw new Error(`Method ${method} not supported`);
    }
  }

  private buildAxiosConfig(
    headers: Record<string, string>,
    isBinary: boolean
  ): AxiosRequestConfig {
    return {
      headers: {
        ...headers,
        "Content-Type": isBinary
          ? "application/octet-stream"
          : "application/json",
      },
      responseType: isBinary ? "arraybuffer" : "json",
    };
  }

  private sanitizeResponseHeaders(
    headers: Record<string, unknown>
  ): ResponseHeaders {
    return Object.fromEntries(
      Object.entries(headers)
        .filter(([, value]) => value !== undefined)
        .map(([key, value]) => [key, String(value)])
    ) as ResponseHeaders;
  }

  private async handleBinaryResponse(
    response: AxiosResponse<unknown>,
    responseHeaders: ResponseHeaders
  ): Promise<APIGatewayProxyResult> {
    const binaryBuffer = Buffer.from(
      new Uint8Array(response.data as ArrayBuffer)
    );
    const fileSizeInBytes = binaryBuffer.length;

    // Lambda 6MB limit. 4.5MB threshold accounts for base64 encoding overhead (~33%)
    const MAX_SAFE_SIZE_BYTES = 4.5 * 1024 * 1024;

    if (fileSizeInBytes > MAX_SAFE_SIZE_BYTES) {
      // File is too large for Lambda response, save to EFS
      const filePath = await this.saveBinaryToEFS(response);
      responseHeaders["X-EFS-File-Path"] = filePath;
      responseHeaders["X-File-Size"] = fileSizeInBytes.toString();

      return {
        statusCode: response.status,
        headers: responseHeaders,
        body: JSON.stringify({
          message: "File saved to EFS due to size limit",
          filePath,
          fileSize: fileSizeInBytes,
        }),
        isBase64Encoded: false,
      };
    }

    // File is small enough, return directly as base64
    return {
      statusCode: response.status,
      headers: responseHeaders,
      body: binaryBuffer.toString("base64"),
      isBase64Encoded: true,
    };
  }

  private buildJsonResponse(
    response: AxiosResponse<unknown>,
    responseHeaders: ResponseHeaders
  ): APIGatewayProxyResult {
    return {
      statusCode: response.status,
      headers: responseHeaders,
      body: JSON.stringify(response.data),
      isBase64Encoded: false,
    };
  }

  public async handleRequest(
    event: APIGatewayEvent
  ): Promise<APIGatewayProxyResult> {
    try {
      const method = event.httpMethod.toUpperCase();

      // Validate HTTP method
      if (!["GET", "POST", "PUT"].includes(method)) {
        return {
          statusCode: 405,
          body: JSON.stringify({ message: `Method ${method} not allowed` }),
        };
      }

      // Parse request
      const body = event.body ? JSON.parse(event.body) : {};
      const queryString = sanitizeQueryStringParams(
        event.queryStringParameters || {}
      );
      const headers = sanitizeHeaders(event.headers);
      const url = `${event.path}?${queryString}`;

      console.log(`${method} ${url}`);

      // Determine response type
      const isBinary =
        headers && headers["Accept"]?.startsWith("application/octet-stream");

      // Execute request
      const axiosConfig = this.buildAxiosConfig(headers, isBinary);
      const response = await this.executeHttpRequest(
        method,
        url,
        body,
        axiosConfig
      );

      console.log(`Response status: ${response.status}`);

      // Build response
      const responseHeaders = this.sanitizeResponseHeaders(response.headers);

      if (isBinary) {
        return await this.handleBinaryResponse(response, responseHeaders);
      }

      return this.buildJsonResponse(response, responseHeaders);
    } catch (error) {
      console.error("Error handling request:", {
        method: event?.httpMethod,
        path: event?.path,
        error: error instanceof Error ? error.message : String(error),
      });

      return {
        statusCode: 500,
        body: JSON.stringify({ message: "Internal Server Error" }),
      };
    }
  }
}
