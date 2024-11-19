import {
  GetSecretValueCommand,
  SecretsManagerClient,
  UpdateSecretCommand,
} from "@aws-sdk/client-secrets-manager";

export default class SecretsManagerService {
  private client = new SecretsManagerClient();

  async getSecret(secretName: string): Promise<string> {
    const command = new GetSecretValueCommand({ SecretId: secretName });
    const data = await this.client.send(command);

    if (data.SecretString) {
      return data.SecretString;
    } else {
      throw new Error(
        `Secret with ID ${secretName} does not contain SecretString`
      );
    }
  }

  async updateSecret(secretId: string, secretString: string): Promise<void> {
    const command = new UpdateSecretCommand({
      SecretId: secretId,
      SecretString: secretString,
    });

    await this.client.send(command);
  }
}
