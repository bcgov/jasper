import { Logger } from "@aws-lambda-powertools/logger";
import { APIGatewayEvent, APIGatewayProxyResult, Context } from "aws-lambda";
import { v4 as uuidv4 } from "uuid";
import ECSService from "../../../services/ecsService";
import SecretsManagerService from "../../../services/secretsManagerService";

export const handler = async (
  event: APIGatewayEvent,
  context: Context
): Promise<APIGatewayProxyResult> => {
  console.log(`Event: ${JSON.stringify(event, null, 2)}`);
  console.log(`Context: ${JSON.stringify(context, null, 2)}`);

  const logger = new Logger({
    serviceName: "auth.rotate-key",
  });

  try {
    logger.info("Rotating verifyKey.");
    await updateSecret();
    logger.info("Successfully rotated verifyKey");

    logger.info("Restarting ECS Services to pickup updated VerifyKey.");
    await restartECSServices();
    logger.info("Restart completed.");

    return {
      statusCode: 200,
      body: JSON.stringify({
        message:
          "Successfully rotated the key and restarted the ECS Service/TD",
      }),
    };
  } catch (error) {
    logger.error(error);

    return {
      statusCode: 500,
      body: JSON.stringify({
        message:
          "Something went wrong when updating the key and restarting the ECS Service/TD",
      }),
    };
  }
};

const updateSecret = async () => {
  const smService = new SecretsManagerService();
  const newGuid = uuidv4();

  await smService.updateSecret(
    process.env.VERIFY_SECRET_NAME!,
    JSON.stringify({ verifyKey: newGuid })
  );
};

const restartECSServices = async () => {
  const ecsService = new ECSService(process.env.CLUSTER_NAME!);

  const services = await ecsService.getECSServices();

  await ecsService.restartServices(services);
};
