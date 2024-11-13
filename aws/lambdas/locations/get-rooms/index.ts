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
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "1051-001",
        flex: "1051"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "1201-001",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "2007-001",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "2031-001",
        flex: "2031"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "2040-001",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "2045-001",
        flex: "2045"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "2048-001",
        flex: "2048"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "3531-001",
        flex: "3531"
      }
    ])
  }
}
