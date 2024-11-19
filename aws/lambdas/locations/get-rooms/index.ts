import { APIGatewayEvent, APIGatewayProxyResult, Context } from "aws-lambda"

export const handler = async (
  event: APIGatewayEvent,
  context: Context
): Promise<APIGatewayProxyResult> => {
  console.log(event, context)

  return {
    statusCode: 200,
    body: JSON.stringify([
      {
        codeType: "COURT_ROOMS",
        code: "00",
        shortDesc: "CRT",
        longDesc: "NIDD-00",
        flex: "NIDD"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "1031-001",
        flex: "1031"
      }
    ])
  }
}
