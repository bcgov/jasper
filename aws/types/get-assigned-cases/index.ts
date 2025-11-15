export interface GetAssignedCasesRequest {
  reasons?: string;
  restrictions?: string;
}

export interface GetAssignedCasesResponse {
  success: boolean;
  data: Case[] | null;
  message: string;
  error?: string;
}

export interface Case {
  appearanceId: string | null;
  physicalFileId: string | null;
  courtFileNumber: string | null;
  courtClass: string | null;
  courtLevel: string | null;
  appearanceDate: string | null;
  styleOfCause: string | null;
  reason: string | null;
  dueDate: string | null;
  judgeId: number | null;
  partId: string | null;
  profPartId: string | null;
  participants: Participant[];
}

export interface Participant {
  fullName: string | null;
  role: string | null;
}
