import { IHttpService } from "./httpService";
import SecretsManagerService from "./secretsManagerService";

export class LookupService {
  u: string;
  p: string;

  constructor(
    private httpService: IHttpService,
    private smService: SecretsManagerService
  ) {}

  async init() {
    console.log(
      `Retrieving credentials for ${process.env.FILE_SERVICES_CLIENT_SECRET_NAME}.`
    );

    const secretStringJson = await this.smService.getSecret(
      process.env.FILE_SERVICES_CLIENT_SECRET_NAME!
    );

    console.log(`Credentials retrieved: ${secretStringJson}`);

    console.log(`Initializing httpService`);

    const { baseUrl, username, password } = JSON.parse(secretStringJson);

    this.u = username;
    this.p = password;

    await this.httpService.init(baseUrl, username, password);

    console.log("httpService initialized.");
  }

  async get(url: string, queryParams?: Record<string, unknown>) {
    console.log(`Sending GET request for ${url}...`);

    console.log(queryParams);

    return await this.httpService.get(url, this.u, this.p);
  }
}
