import { Activity } from './Activity';
import { CalendarDay } from './CalendarDay';
import { Presider } from './Presider';

export interface CourtCalendarPresidersSchedule {
  days: CalendarDay[];
  activities: Activity[];
  presiders: Presider[];
}
