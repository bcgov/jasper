import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";
import { ApiService } from "../../../services/apiService";

const X_TARGET_APP_HEADER = "x-target-app";

const getHeader = (
  headers: Record<string, string | undefined> | null,
  name: string,
): string | undefined => {
  if (!headers) {
    return undefined;
  }

  const found = Object.entries(headers).find(
    ([key]) => key.toLowerCase() === name.toLowerCase(),
  );

  return found?.[1];
};

export const handler = async (
  event: APIGatewayEvent,
): Promise<APIGatewayProxyResult> => {
  const targetApp = getHeader(
    event.headers,
    X_TARGET_APP_HEADER,
  )?.toUpperCase();

  let credentialsSecret: string;

  switch (targetApp) {
    case "TD":
      credentialsSecret = process.env.TD_SECRET_NAME!;
      break;
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
  await apiService.initialize();

  const result = await apiService.handleRequest(event, {
    forwardAuthorization: targetApp === "TD",
  });

  return result;
};
