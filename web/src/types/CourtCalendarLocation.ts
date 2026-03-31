import { CourtCalendarActivity } from './CourtCalendarActivity';

export interface CourtCalendarLocation {
  locationId: number;
  locationShortName: string;
  activities: CourtCalendarActivity[];
}
