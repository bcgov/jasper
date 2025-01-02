import { APIGatewayEvent, APIGatewayProxyResult, Context } from "aws-lambda";
import { HttpService } from "../../../services/httpService";
import SecretsManagerService from "../../../services/secretsManagerService";

export const handler = async (
  event: APIGatewayEvent,
  context: Context
): Promise<APIGatewayProxyResult> => {
  console.log(event, context);

  try {
    const smService = new SecretsManagerService();
    const secretStringJson = await smService.getSecret(
      process.env.FILE_SERVICES_CLIENT_SECRET_NAME!
    );
    const { baseUrl, username, password } = JSON.parse(secretStringJson);
    const httpService = new HttpService();

    await httpService.init(baseUrl, username, password);
    const queryParams = event.queryStringParameters || {};

    const data = await httpService.get(event.path, queryParams);

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
