import { Handler } from "aws-lambda";
import { AssignedCasesService } from "../../../services/assignedCasesService";
import {
  GetAssignedCasesRequest,
  GetAssignedCasesResponse,
} from "../../../types/get-assigned-cases";

export const handler: Handler<
  GetAssignedCasesRequest,
  GetAssignedCasesResponse
> = async (event) => {
  const assignedCasesService = new AssignedCasesService();
  await assignedCasesService.initialize();

  const response = await assignedCasesService.getAssignedCases(event);

  return response;
};
