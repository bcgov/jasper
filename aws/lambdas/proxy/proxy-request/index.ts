import { APIGatewayEvent, APIGatewayProxyResult } from "aws-lambda";

export const handler = async (
  event: APIGatewayEvent
): Promise<APIGatewayProxyResult> => {
  console.log(`Received ${event.httpMethod} request for ${event.path}`);
  console.log(event);

  return {
    statusCode: 200,
    body: "Request successful!",
  };

  // const catsApiService = new CatsApiService();
  // return await catsApiService.handleRequest(event);
};
