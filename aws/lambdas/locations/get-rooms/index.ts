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
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "3551-001",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "3585-001",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "4681-001",
        flex: "4681"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "4781-001",
        flex: "4781"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "4801-001",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "4891-001",
        flex: "4891"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "4901-001",
        flex: "4901"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "4971-001",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "5671-001",
        flex: "5671"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "5731-001",
        flex: "5731"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "5971-001",
        flex: "5971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "6011-001",
        flex: "6011"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "6012-001",
        flex: "6012"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "702-001",
        flex: "702"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "7999-001",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "ADJU-001",
        flex: "ADJU"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "CAKE-001",
        flex: "CAKE"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "CAVA-001",
        flex: "CAVA"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "LECR-001",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "CRT",
        longDesc: "SHER-001",
        flex: "SHER"
      },
      {
        codeType: "COURT_ROOMS",
        code: "001",
        shortDesc: "VCR",
        longDesc: "KAIN-001",
        flex: "KAIN"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "1201-002",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "2007-002",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "2040-002",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "2048-002",
        flex: "2048"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "3531-002",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "3551-002",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "3585-002",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "4711-002",
        flex: "4711"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "4781-002",
        flex: "4781"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "4801-002",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "4971-002",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "5671-002",
        flex: "5671"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "6011-002",
        flex: "6011"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "6012-002",
        flex: "6012"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "7999-002",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "002",
        shortDesc: "CRT",
        longDesc: "LECR-002",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "1201-003",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "2007-003",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "2040-003",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "2048-003",
        flex: "2048"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "3531-003",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "3551-003",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "4801-003",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "4971-003",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "5671-003",
        flex: "5671"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "7999-003",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "003",
        shortDesc: "CRT",
        longDesc: "LECR-003",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "1201-004",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "2007-004",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "2040-004",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "3531-004",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "3551-004",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "4801-004",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "4971-004",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "7999-004",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "004",
        shortDesc: "CRT",
        longDesc: "LECR-004",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "2007-005",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "2040-005",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "3531-005",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "3551-005",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "4801-005",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "4971-005",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "7999-005",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "005",
        shortDesc: "CRT",
        longDesc: "LECR-005",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "006",
        shortDesc: "CRT",
        longDesc: "2007-006",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "006",
        shortDesc: "CRT",
        longDesc: "3531-006",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "006",
        shortDesc: "CRT",
        longDesc: "3551-006",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "006",
        shortDesc: "CRT",
        longDesc: "4801-006",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "006",
        shortDesc: "CRT",
        longDesc: "4971-006",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "006",
        shortDesc: "CRT",
        longDesc: "7999-006",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "006",
        shortDesc: "CRT",
        longDesc: "LECR-006",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "007",
        shortDesc: "CRT",
        longDesc: "2007-007",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "007",
        shortDesc: "CRT",
        longDesc: "3531-007",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "007",
        shortDesc: "CRT",
        longDesc: "3551-007",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "007",
        shortDesc: "CRT",
        longDesc: "4801-007",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "007",
        shortDesc: "CRT",
        longDesc: "4971-007",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "007",
        shortDesc: "CRT",
        longDesc: "7999-007",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "007",
        shortDesc: "CRT",
        longDesc: "LECR-007",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "008",
        shortDesc: "CRT",
        longDesc: "2007-008",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "008",
        shortDesc: "CRT",
        longDesc: "3531-008",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "008",
        shortDesc: "CRT",
        longDesc: "3551-008",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "008",
        shortDesc: "CRT",
        longDesc: "7999-008",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "008",
        shortDesc: "CRT",
        longDesc: "LECR-008",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "008",
        shortDesc: "WTM",
        longDesc: "4801-008",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "008",
        shortDesc: "WTM",
        longDesc: "4971-008",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "2007-009",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "3531-009",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "3551-009",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "4801-009",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "4891-009",
        flex: "4891"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "4971-009",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "7999-009",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "009",
        shortDesc: "CRT",
        longDesc: "LECR-009",
        flex: "LECR"
      },
      {
        codeType: "COURT_ROOMS",
        code: "010",
        shortDesc: "CRT",
        longDesc: "3531-010",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "010",
        shortDesc: "CRT",
        longDesc: "7999-010",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "010",
        shortDesc: "WTM",
        longDesc: "4801-010",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "010",
        shortDesc: "WTM",
        longDesc: "4971-010",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "011",
        shortDesc: "CRT",
        longDesc: "3531-011",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "011",
        shortDesc: "CRT",
        longDesc: "4801-011",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "011",
        shortDesc: "CRT",
        longDesc: "7999-011",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "012",
        shortDesc: "CRT",
        longDesc: "3531-012",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "012",
        shortDesc: "CRT",
        longDesc: "4801-012",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "012",
        shortDesc: "CRT",
        longDesc: "7999-012",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "013",
        shortDesc: "CRT",
        longDesc: "4801-013",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "014",
        shortDesc: "CRT",
        longDesc: "4801-014",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "014",
        shortDesc: "CRT",
        longDesc: "7999-014",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "015",
        shortDesc: "CRT",
        longDesc: "4801-015",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "016",
        shortDesc: "CRT",
        longDesc: "4801-016",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "017",
        shortDesc: "CRT",
        longDesc: "4801-017",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "018",
        shortDesc: "CRT",
        longDesc: "4801-018",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "019",
        shortDesc: "CRT",
        longDesc: "4801-019",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "021",
        shortDesc: "CRT",
        longDesc: "4801-021",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "022",
        shortDesc: "CRT",
        longDesc: "4801-022",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "023",
        shortDesc: "CRT",
        longDesc: "4801-023",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "024",
        shortDesc: "CRT",
        longDesc: "4801-024",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "025",
        shortDesc: "CRT",
        longDesc: "4801-025",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "026",
        shortDesc: "CRT",
        longDesc: "4801-026",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "027",
        shortDesc: "CRT",
        longDesc: "4801-027",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "028",
        shortDesc: "CRT",
        longDesc: "4801-028",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "029",
        shortDesc: "CRT",
        longDesc: "4801-029",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "031",
        shortDesc: "CRT",
        longDesc: "4801-031",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "032",
        shortDesc: "CRT",
        longDesc: "4801-032",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "033",
        shortDesc: "CRT",
        longDesc: "4801-033",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "034",
        shortDesc: "CRT",
        longDesc: "4801-034",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "035",
        shortDesc: "CRT",
        longDesc: "4801-035",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "036",
        shortDesc: "CRT",
        longDesc: "4801-036",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "037",
        shortDesc: "CRT",
        longDesc: "4801-037",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "038",
        shortDesc: "CRT",
        longDesc: "4801-038",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "039",
        shortDesc: "CRT",
        longDesc: "4801-039",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "041",
        shortDesc: "CRT",
        longDesc: "4801-041",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "042",
        shortDesc: "CRT",
        longDesc: "4801-042",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "043",
        shortDesc: "CRT",
        longDesc: "4801-043",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "044",
        shortDesc: "CRT",
        longDesc: "4801-044",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "045",
        shortDesc: "CRT",
        longDesc: "4801-045",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "046",
        shortDesc: "CRT",
        longDesc: "4801-046",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "047",
        shortDesc: "CRT",
        longDesc: "4801-047",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "048",
        shortDesc: "CRT",
        longDesc: "4801-048",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "049",
        shortDesc: "CRT",
        longDesc: "4801-049",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "051",
        shortDesc: "CRT",
        longDesc: "4801-051",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "052",
        shortDesc: "CRT",
        longDesc: "4801-052",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "053",
        shortDesc: "CRT",
        longDesc: "4801-053",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "054",
        shortDesc: "CRT",
        longDesc: "4801-054",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "055",
        shortDesc: "CRT",
        longDesc: "4801-055",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "1",
        shortDesc: "CRT",
        longDesc: "4821-1",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "10",
        shortDesc: "CRT",
        longDesc: "4821-10",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "100",
        shortDesc: "CRT",
        longDesc: "1201-100",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "100",
        shortDesc: "CRT",
        longDesc: "3585-100",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "100",
        shortDesc: "CRT",
        longDesc: "5961-100",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "100",
        shortDesc: "MTG",
        longDesc: "KCCA-100",
        flex: "KCCA"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "1091-101",
        flex: "1091"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "3585-101",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "4801-101",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "4971-101",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "5961-101",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA01-101",
        flex: "NA01"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA02-101",
        flex: "NA02"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA03-101",
        flex: "NA03"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA04-101",
        flex: "NA04"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA05-101",
        flex: "NA05"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA06-101",
        flex: "NA06"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA07-101",
        flex: "NA07"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA08-101",
        flex: "NA08"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA09-101",
        flex: "NA09"
      },
      {
        codeType: "COURT_ROOMS",
        code: "101",
        shortDesc: "CRT",
        longDesc: "NA10-101",
        flex: "NA10"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "1091-102",
        flex: "1091"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "5961-102",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA01-102",
        flex: "NA01"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA02-102",
        flex: "NA02"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA03-102",
        flex: "NA03"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA04-102",
        flex: "NA04"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA05-102",
        flex: "NA05"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA06-102",
        flex: "NA06"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA07-102",
        flex: "NA07"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA08-102",
        flex: "NA08"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA09-102",
        flex: "NA09"
      },
      {
        codeType: "COURT_ROOMS",
        code: "102",
        shortDesc: "CRT",
        longDesc: "NA10-102",
        flex: "NA10"
      },
      {
        codeType: "COURT_ROOMS",
        code: "103",
        shortDesc: "CRT",
        longDesc: "3585-103",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "103",
        shortDesc: "CRT",
        longDesc: "5961-103",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "104",
        shortDesc: "CRT",
        longDesc: "5961-104",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "104",
        shortDesc: "CRT",
        longDesc: "KSS-104",
        flex: "KSS"
      },
      {
        codeType: "COURT_ROOMS",
        code: "105",
        shortDesc: "CRT",
        longDesc: "2040-105",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "105",
        shortDesc: "CRT",
        longDesc: "5961-105",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "11",
        shortDesc: "CRT",
        longDesc: "4821-11",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "12",
        shortDesc: "CRT",
        longDesc: "4821-12",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "120",
        shortDesc: "CRT",
        longDesc: "4971-120",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "1205A",
        shortDesc: "CRT",
        longDesc: "KPU-1205A",
        flex: "KPU"
      },
      {
        codeType: "COURT_ROOMS",
        code: "1205C",
        shortDesc: "CRT",
        longDesc: "KPU-1205C",
        flex: "KPU"
      },
      {
        codeType: "COURT_ROOMS",
        code: "125",
        shortDesc: "CRT",
        longDesc: "1051-125",
        flex: "1051"
      },
      {
        codeType: "COURT_ROOMS",
        code: "12A",
        shortDesc: "CRT",
        longDesc: "2040-12A",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "130",
        shortDesc: "CRT",
        longDesc: "KSS-130",
        flex: "KSS"
      },
      {
        codeType: "COURT_ROOMS",
        code: "132",
        shortDesc: "CRT",
        longDesc: "KSS-132",
        flex: "KSS"
      },
      {
        codeType: "COURT_ROOMS",
        code: "2",
        shortDesc: "CRT",
        longDesc: "4821-2",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "200",
        shortDesc: "CRT",
        longDesc: "2040-200",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "200",
        shortDesc: "CRT",
        longDesc: "5961-200",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "200",
        shortDesc: "CRT",
        longDesc: "6011-200",
        flex: "6011"
      },
      {
        codeType: "COURT_ROOMS",
        code: "201",
        shortDesc: "CRT",
        longDesc: "3561-201",
        flex: "3561"
      },
      {
        codeType: "COURT_ROOMS",
        code: "201",
        shortDesc: "CRT",
        longDesc: "5961-201",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "202",
        shortDesc: "CRT",
        longDesc: "3561-202",
        flex: "3561"
      },
      {
        codeType: "COURT_ROOMS",
        code: "202",
        shortDesc: "CRT",
        longDesc: "5961-202",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "203",
        shortDesc: "CRT",
        longDesc: "3561-203",
        flex: "3561"
      },
      {
        codeType: "COURT_ROOMS",
        code: "204",
        shortDesc: "HGR",
        longDesc: "6011-204",
        flex: "6011"
      },
      {
        codeType: "COURT_ROOMS",
        code: "218",
        shortDesc: "CRT",
        longDesc: "5771-218",
        flex: "5771"
      },
      {
        codeType: "COURT_ROOMS",
        code: "220",
        shortDesc: "SCR",
        longDesc: "6011-220",
        flex: "6011"
      },
      {
        codeType: "COURT_ROOMS",
        code: "3",
        shortDesc: "CRT",
        longDesc: "4821-3",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "300",
        shortDesc: "CRT",
        longDesc: "2040-300",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "300",
        shortDesc: "CRT",
        longDesc: "4971-300",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "300",
        shortDesc: "CRT",
        longDesc: "5961-300",
        flex: "5961"
      },
      {
        codeType: "COURT_ROOMS",
        code: "301",
        shortDesc: "CRT",
        longDesc: "1201-301",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "302",
        shortDesc: "CRT",
        longDesc: "1201-302",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "303",
        shortDesc: "CRT",
        longDesc: "1201-303",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "311",
        shortDesc: "CRT",
        longDesc: "3585-311",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "4",
        shortDesc: "CRT",
        longDesc: "4821-4",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "401",
        shortDesc: "CRT",
        longDesc: "1201-401",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "402",
        shortDesc: "CRT",
        longDesc: "1201-402",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "403",
        shortDesc: "CRT",
        longDesc: "1201-403",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "411",
        shortDesc: "CRT",
        longDesc: "3581-411",
        flex: "3581"
      },
      {
        codeType: "COURT_ROOMS",
        code: "45534",
        shortDesc: "CRT",
        longDesc: "4971-45534",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "5",
        shortDesc: "CRT",
        longDesc: "4821-5",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "500",
        shortDesc: "CRT",
        longDesc: "4971-500",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "512",
        shortDesc: "CRT",
        longDesc: "7999-512",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "533",
        shortDesc: "HGR",
        longDesc: "1201-533",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "6",
        shortDesc: "CRT",
        longDesc: "4821-6",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "7",
        shortDesc: "CRT",
        longDesc: "4821-7",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "766",
        shortDesc: "CRT",
        longDesc: "4971-766",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "8",
        shortDesc: "CRT",
        longDesc: "4821-8",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "9",
        shortDesc: "CRT",
        longDesc: "4821-9",
        flex: "4821"
      },
      {
        codeType: "COURT_ROOMS",
        code: "900",
        shortDesc: "CRT",
        longDesc: "2040-900",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "999",
        shortDesc: "CRT",
        longDesc: "3551-999",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "999",
        shortDesc: "CRT",
        longDesc: "7999-999",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "BLAH",
        shortDesc: "CRT",
        longDesc: "4801-BLAH",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "BLAH",
        shortDesc: "CRT",
        longDesc: "4971-BLAH",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "CHB",
        shortDesc: "CRT",
        longDesc: "1201-CHB",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JCM",
        shortDesc: "ADM",
        longDesc: "3585-JCM",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JCM",
        shortDesc: "CRT",
        longDesc: "4801-JCM",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JJP1",
        shortDesc: "CRT",
        longDesc: "2041-JJP1",
        flex: "2041"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JJP2",
        shortDesc: "CRT",
        longDesc: "2041-JJP2",
        flex: "2041"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JJP3",
        shortDesc: "CRT",
        longDesc: "2041-JJP3",
        flex: "2041"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JJP4",
        shortDesc: "CRT",
        longDesc: "2041-JJP4",
        flex: "2041"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JUNK",
        shortDesc: "CRT",
        longDesc: "4801-JUNK",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "JUNK",
        shortDesc: "CRT",
        longDesc: "4971-JUNK",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "NA",
        shortDesc: "CRT",
        longDesc: "SRES-NA",
        flex: "SRES"
      },
      {
        codeType: "COURT_ROOMS",
        code: "OTH",
        shortDesc: "CRT",
        longDesc: "4801-OTH",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "RED",
        shortDesc: "CRT",
        longDesc: "4971-RED",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "1201-REG",
        flex: "1201"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "1207-REG",
        flex: "1207"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "2007-REG",
        flex: "2007"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "2040-REG",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "2045-REG",
        flex: "2045"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "3531-REG",
        flex: "3531"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "3551-REG",
        flex: "3551"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "3585-REG",
        flex: "3585"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "4801-REG",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "4971-REG",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "6011-REG",
        flex: "6011"
      },
      {
        codeType: "COURT_ROOMS",
        code: "REG",
        shortDesc: "CRT",
        longDesc: "7999-REG",
        flex: "7999"
      },
      {
        codeType: "COURT_ROOMS",
        code: "TAC",
        shortDesc: "WTM",
        longDesc: "2040-TAC",
        flex: "2040"
      },
      {
        codeType: "COURT_ROOMS",
        code: "TD",
        shortDesc: "CRT",
        longDesc: "2045-TD",
        flex: "2045"
      },
      {
        codeType: "COURT_ROOMS",
        code: "TEST",
        shortDesc: "CRT",
        longDesc: "4801-TEST",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "TEST",
        shortDesc: "CRT",
        longDesc: "4971-TEST",
        flex: "4971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "TMP",
        shortDesc: "CRT",
        longDesc: "4801-TMP",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "VC",
        shortDesc: "VCR",
        longDesc: "C402-VC",
        flex: "C402"
      },
      {
        codeType: "COURT_ROOMS",
        code: "VR1",
        shortDesc: "CRT",
        longDesc: "4801-VR1",
        flex: "4801"
      },
      {
        codeType: "COURT_ROOMS",
        code: "VR1",
        shortDesc: "CRT",
        longDesc: "5971-VR1",
        flex: "5971"
      },
      {
        codeType: "COURT_ROOMS",
        code: "VR2",
        shortDesc: "CRT",
        longDesc: "5971-VR2",
        flex: "5971"
      }
    ])
  }
}
