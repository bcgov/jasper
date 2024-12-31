import { JudicialCalendarActivity } from './JudicialCalendarActivity';

export interface JudicialCalendarAssignment {
  id: number;
  judgeId: number;
  locationId: number;
  locationName: string;
  date: string;
  activityCode: string;
  activityDisplayCode: string;
  activityDescription: string;
  isCommentRequired: boolean;
  activityClassCode: string;
  activityClassDescription: string;
  isVideo: boolean;
  isExtraSeniorDay: boolean;
  force: boolean;
  ignoreWeekendUpdate: boolean;
  isJj: boolean;
  isPcj: boolean;
  isJp: boolean;
  isOther: boolean;
  isIar: boolean;
  removeFromActivityAm: boolean;
  removeFromActivityPm: boolean;
  activityAm: JudicialCalendarActivity;
  activityPm: JudicialCalendarActivity;
}
