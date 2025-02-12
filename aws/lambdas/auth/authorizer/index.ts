import { Logger } from "@aws-lambda-powertools/logger";
import {
  APIGatewayAuthorizerResult,
  APIGatewayRequestAuthorizerEvent,
  Context,
  PolicyDocument,
  StatementEffect,
} from "aws-lambda";
import SecretsManagerService from "../../../services/secretsManagerService";
import { replaceWithWildcard } from "../../../util";

const X_ORIGIN_VERIFY_HEADER = "x-origin-verify";

export const handler = async (
  event: APIGatewayRequestAuthorizerEvent,
  context: Context
): Promise<APIGatewayAuthorizerResult> => {
  console.log(`Event: ${JSON.stringify(event, null, 2)}`);
  console.log(`Context: ${JSON.stringify(context, null, 2)}`);

  const logger = new Logger({
    serviceName: "auth.authorizer",
  });

  try {
    if (!event.headers) {
      logger.error("headers is missing.");
      throw new Error("Error: invalid token");
    }

    // x-verify-origin should be set by the caller
    if (!(X_ORIGIN_VERIFY_HEADER in event.headers)) {
      logger.error(`${X_ORIGIN_VERIFY_HEADER} not found in headers.`);
      throw new Error("Error: invalid token");
    }

    // Extract the token from the request
    const verifyToken = event.headers[X_ORIGIN_VERIFY_HEADER];
    const smService = new SecretsManagerService();
    const secretStringJson = await smService.getSecret(
      process.env.VERIFY_SECRET_NAME!
    );

    let verifyTokenfromSecretManager = "";
    if (secretStringJson) {
      verifyTokenfromSecretManager = JSON.parse(secretStringJson).verifyKey;
      logger.info(
        "Authorization token from secret manager",
        verifyTokenfromSecretManager
      );
    } else {
      logger.error("Secret not found in secret manager");
      throw new Error("Error: invalid token");
    }

    if (verifyToken !== verifyTokenfromSecretManager) {
      logger.error("Authorization token not valid");
      throw new Error("Error: invalid token");
    }

    const policy = generatePolicy("Allow", event.methodArn);

    logger.info("Authorized");
    logger.info(JSON.stringify(policy));

    return policy;
  } catch (error) {
    logger.error(error);

    throw new Error("Unauthorized");
  }
};

const generatePolicy = (
  effect: StatementEffect,
  resource: string
): APIGatewayAuthorizerResult => {
  const policyDocument: PolicyDocument = {
    Version: "2012-10-17",
    Statement: [
      {
        Action: "execute-api:Invoke",
        Effect: effect,
        Resource: replaceWithWildcard(resource),
      },
    ],
  };

  const authResponse: APIGatewayAuthorizerResult = {
    principalId: "user",
    context: {
      correlation_id: "generic",
    },
    policyDocument,
  };

  return authResponse;
};
