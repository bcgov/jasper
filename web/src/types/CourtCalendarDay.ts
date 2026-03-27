import { CourtCalendarLocation } from './CourtCalendarLocation';

export interface CourtCalendarDay {
  date: string;
  isWeekend: boolean;
  locations: CourtCalendarLocation[];
}
