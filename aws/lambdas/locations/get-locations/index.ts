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
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10232.0001",
        shortDesc: "1061",
        longDesc: "Ganges Provincial Court",
        flex: "Y"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10233.0001",
        shortDesc: "1071",
        longDesc: "Gold River Provincial Court",
        flex: "Y"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10234.0001",
        shortDesc: "1111",
        longDesc: "Parksville Provincial Court",
        flex: "Y"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10235.0001",
        shortDesc: "1121",
        longDesc: "Port Alberni Law Courts",
        flex: "Y"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10236.0001",
        shortDesc: "1141",
        longDesc: "Port Hardy Law Courts",
        flex: "Y"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10237.0001",
        shortDesc: "1145",
        longDesc: "Powell River Law Courts",
        flex: "Y"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10238.0001",
        shortDesc: "1151",
        longDesc: "Sidney Provincial Court",
        flex: "Y"
      },
      {
        codeType: "COURT_LOCATIONS",
        code: "10239.0001",
        shortDesc: "1171",
        longDesc: "Tahsis Provincial Court",
        flex: "Y"
      }
    ])
  }
}
