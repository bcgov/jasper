import { CourtCalendarActivity } from './CourtCalendarActivity';

export interface CourtCalendarLocation {
  locationId: number;
  locationName: string;
  activities: CourtCalendarActivity[];
}
