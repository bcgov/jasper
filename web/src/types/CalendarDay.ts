import { CalendarDayActivity } from './CalendarDayActivity';

export interface CalendarDay {
  date: string;
  isWeekend: boolean;
  showCourtList: boolean;
  activities: CalendarDayActivity[];
}
