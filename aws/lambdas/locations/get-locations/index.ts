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
        codeType: "COURT_LOCATIONS",
        code: "10230.0001",
        shortDesc: "1011",
        longDesc: "Alert Bay",
        flex: "N"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10231.0001",
        shortDesc: "1051",
        longDesc: "Duncan Law Courts",
        flex: "Y"
      }
    ])
  }
}
