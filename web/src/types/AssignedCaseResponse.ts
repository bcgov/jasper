import { Case } from './Case';

export type AssignedCaseResponse = {
  dueDateEditingJudgeId?: number | null;
  reservedJudgments: Case[];
  scheduledContinuations: Case[];
  others: Case[];
  futureAssigned: Case[];
};
