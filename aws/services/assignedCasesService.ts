import { AxiosRequestConfig } from "axios";
import {
  Case,
  GetAssignedCasesRequest,
  GetAssignedCasesResponse,
} from "../types/get-assigned-cases";
import HttpService, { IHttpService } from "./httpService";
import SecretsManagerService from "./secretsManagerService";

export class AssignedCasesService {
  protected httpService: IHttpService;
  protected smService: SecretsManagerService;
  private isInitialized = false;

  constructor() {
    this.smService = new SecretsManagerService();
    this.httpService = new HttpService();
  }

  public async initialize(): Promise<void> {
    const pcssSecretName = process.env.PCSS_SECRET_NAME;
    const mtlsSecretName = process.env.MTLS_SECRET_NAME;

    if (!pcssSecretName || !mtlsSecretName) {
      throw new Error(
        "Missing required environment variables: PCSS_SECRET_NAME or MTLS_SECRET_NAME"
      );
    }

    const credentialsSecret = await this.smService.getSecret(pcssSecretName);
    const mtlsSecret = await this.smService.getSecret(mtlsSecretName);

    await this.httpService.init(credentialsSecret, mtlsSecret);
    this.isInitialized = true;
    console.log("httpService initialized successfully");
  }

  public async getAssignedCases(
    request: GetAssignedCasesRequest
  ): Promise<GetAssignedCasesResponse> {
    if (!this.isInitialized) {
      throw new Error(
        "AssignedCasesService not initialized. Call initialize() first."
      );
    }

    const startTime = Date.now();
    const params = new URLSearchParams({
      reasons: request.reasons ?? "",
      restrictions: request.restrictions ?? "",
    });

    const url = `/api/calendar/judges/upcomingSeizedAssignedCases?${params}`;

    const axiosConfig: AxiosRequestConfig = {
      headers: {
        "Content-Type": "application/json",
      },
      responseType: "json",
    };

    try {
      console.log("Fetching assigned cases from PCSS API:", url.toString());

      const response = await this.httpService.get<Case[]>(url, axiosConfig);
      const { data = [], status } = response || {};

      const durationMs = Date.now() - startTime;
      const durationSeconds = (durationMs / 1000).toFixed(2);

      console.log("Successfully retrieved scheduled cases", {
        count: data.length,
        statusCode: status,
        duration: `${durationSeconds}s`,
        durationMs,
      });

      return {
        data,
        success: true,
        message: `Retrieved ${data.length} scheduled cases`,
      };
    } catch (error) {
      const durationMs = Date.now() - startTime;
      const durationSeconds = (durationMs / 1000).toFixed(2);

      console.error("Error fetching assigned cases:", {
        error: error instanceof Error ? error.message : String(error),
        duration: `${durationSeconds}s`,
        durationMs,
      });

      return {
        data: [],
        success: false,
        message:
          error instanceof Error
            ? error.message
            : "Failed to retrieve scheduled cases",
      };
    }
  }
}
