import { CalendarDayActivity } from './CalendarDayActivity';
import { JudicialCalendarAssignment } from './JudicialCalendarAssignment';

export interface CalendarDay {
  rotaInitials: string;
  start: string;
  showAM: boolean;
  showPM: boolean;
  showPMLocation: boolean;
  judgeId: number;
  date: string;
  name: string;
  positionTypeCode: string;
  positionTypeDescription: string;
  positionCode: string;
  positionDescription: string;
  positionStatusCode: string;
  positionStatusDescription: string;
  isPresider: boolean;
  isJudge: boolean;
  isAdmin: boolean;
  restrictions: any[];
  hasRestrictions: boolean;
  hasAdjudicatorIssues: boolean;
  haveJudgeDetails: any[];
  assignment: JudicialCalendarAssignment;
  activities: any[];
}

export interface CalendarDayV2 {
  date: string;
  isWeekend: boolean;
  showCourtList: boolean;
  activities: CalendarDayActivity[];
}
