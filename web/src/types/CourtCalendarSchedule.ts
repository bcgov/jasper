import { Activity } from './Activity';
import { CalendarDay } from './CalendarDay';
import { Presider } from './Presider';

export interface CourtCalendarSchedule {
  days: CalendarDay[];
  activities: Activity[];
  presiders: Presider[];
}
