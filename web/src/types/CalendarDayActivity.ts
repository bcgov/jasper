import { AdjudicatorRestriction } from './AdjudicatorRestriction';

export interface CalendarDayActivity {
  locationId?: number;
  locationName: string;
  locationShortName: string;
  activityCode: string;
  activityDisplayCode: string;
  activityDescription: string;
  activityClassDescription: string;
  isRemote: boolean;
  roomCode: string;
  period: string;
  filesCount: number;
  continuationsCount: number;
  showDars: boolean;
  restrictions: AdjudicatorRestriction[];
  judgeId: number;
  judgeInitials: string;
  isJudgeBorrowed: boolean;
  isJudgeAway: boolean;
}
