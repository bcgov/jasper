export type Case = {
  id: string;
  appearanceId: string;
  appearanceDate: string;
  judgeId?: number;
  courtClass: string;
  physicalFileId: string;
  courtFileNumber: string;
  fileNumber: string;
  ageInDays: number;
  styleOfCause: string;
  reason: string;
  restrictionCode?: string | null;
  dueDate: string | null;
  overrideDueDate?: string | null;
  effectiveDueDate?: string | null;
  partId: string;
  updatedDate?: string;
};
