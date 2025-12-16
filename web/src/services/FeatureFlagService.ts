import { HttpService } from './HttpService';

export class FeatureFlagService {
  constructor(private readonly httpService: HttpService) {}

  async getFeatureFlags(): Promise<Record<string, boolean>> {
    return await this.httpService.get<Record<string, boolean>>(
      `api/featureflag`
    );
  }
}
