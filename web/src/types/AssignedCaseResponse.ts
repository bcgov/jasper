import { Case } from './Case';

export type AssignedCaseResponse = {
  reservedJudgments: Case[];
  scheduledContinuations: Case[];
  others: Case[];
  futureAssigned: Case[];
};
