import {
  GetSecretValueCommand,
  SecretsManagerClient
} from "@aws-sdk/client-secrets-manager"

const secretsManagerClient = new SecretsManagerClient()

export const getSecret = async (secretName: string): Promise<string> => {
  try {
    const command = new GetSecretValueCommand({ SecretId: secretName })
    const data = await secretsManagerClient.send(command)

    if (data.SecretString) {
      return data.SecretString
    } else {
      throw new Error(
        `Secret with ID ${secretName} does not contain SecretString`
      )
    }
  } catch (error) {
    console.error(`Error retrieving secret ${secretName}:`, error)
    throw error
  }
}
