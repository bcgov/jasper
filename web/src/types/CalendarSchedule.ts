import { Activity } from './Activity';
import { CalendarDay } from './CalendarDay';
import { Presider } from './Presider';

export interface CalendarSchedule {
  today: CalendarDay;
  days: CalendarDay[];
  schedule: CalendarDay[];
  activities: Activity[];
  presiders: Presider[];
}
