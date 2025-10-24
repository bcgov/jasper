import { Case } from './Case';

export type AssignedCaseResponse = {
  reservedJudgments: Case[];
  scheduledContinuations: Case[];
};
