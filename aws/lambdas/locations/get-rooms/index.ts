import { APIGatewayEvent, APIGatewayProxyResult, Context } from "aws-lambda";
import { HttpService } from "../../../services/httpService";
import { LookupService } from "../../../services/lookupService";
import SecretsManagerService from "../../../services/secretsManagerService";

export const handler = async (
  event: APIGatewayEvent,
  context: Context
): Promise<APIGatewayProxyResult> => {
  console.log(event, context);

  try {
    const smService = new SecretsManagerService();
    const httpService = new HttpService();
    const lookupService = new LookupService(httpService, smService);

    await lookupService.init();

    const queryParams = event.queryStringParameters || {};

    const data = await lookupService.get(event.path, queryParams);

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
};
