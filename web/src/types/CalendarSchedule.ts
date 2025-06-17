import { Activity } from './Activity';
import { CalendarDay, CalendarDayV2 } from './CalendarDay';
import { Presider } from './Presider';

export interface CalendarSchedule {
  today: CalendarDay;
  days: CalendarDayV2[];
  schedule: CalendarDay[];
  activities: Activity[];
  presiders: Presider[];
}
