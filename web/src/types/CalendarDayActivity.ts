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
  judgeName: string;
  judgeInitials: string;
  isJudgeBorrowed: boolean;
  isJudgeAway: boolean;
}

export interface JudgeActivity {
  judgeInitials: string;
  judgeName: string;
  activities: CalendarDayActivity[];
}

export interface LocationGroup {
  locationName: string;
  judgeActivities: JudgeActivity[];
}
