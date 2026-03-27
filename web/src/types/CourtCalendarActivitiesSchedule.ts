import { Activity } from './Activity';
import { CourtCalendarDay } from './CourtCalendarDay';

export interface CourtCalendarActivitiesSchedule {
  days: CourtCalendarDay[];
  activities: Activity[];
}
