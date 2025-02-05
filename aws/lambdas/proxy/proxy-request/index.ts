import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import { ApiService } from "../../../services/apiService";

const X_TARGET_APP_HEADER = "x-target-app";

export const handler = async (
  event: APIGatewayEvent
): Promise<APIGatewayProxyResult> => {
  const targetApp = event.headers[X_TARGET_APP_HEADER];

  console.log(
    `Received "${event.httpMethod}" request for "${targetApp}" with route "${event.path}"`
  );
  console.log(event);

  let credentialsSecret: string;

  switch (targetApp) {
    case "DARS":
      credentialsSecret = process.env.DARS_SECRET_NAME!;
      break;
    case "PCSS":
      credentialsSecret = process.env.PCSS_SECRET_NAME!;
      break;
    default:
      // Defaults to "lookup" when targetApp is not found
      credentialsSecret = process.env.FILE_SERVICES_CLIENT_SECRET_NAME!;
      break;
  }

  const apiService = new ApiService(credentialsSecret);
  const result = await apiService.handleRequest(event);

  console.log(result);

  return result;
};
