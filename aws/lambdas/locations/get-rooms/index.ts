import { APIGatewayEvent, APIGatewayProxyResult, Context } from "aws-lambda";
import HttpService from "../../../services/httpService";

export const handler = async (
  event: APIGatewayEvent,
  context: Context
): Promise<APIGatewayProxyResult> => {
  console.log(event, context);

  try {
    const http = new HttpService();
    await http.init("https://wsgw.dev.jag.gov.bc.ca/dev");
    const result = await http.get("/TestService");

    console.log(result);

    return {
      statusCode: 200,
      body: JSON.stringify([
        {
          codeType: "COURT_ROOMS",
          code: "00",
          shortDesc: "CRT",
          longDesc: "NIDD-00",
          flex: "NIDD",
        },
        {
          codeType: "COURT_ROOMS",
          code: "001",
          shortDesc: "CRT",
          longDesc: "1031-001",
          flex: "1031",
        },
      ]),
    };
  } catch (error) {
    console.error(error);
    throw error;
  }
};
