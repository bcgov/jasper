import { CourtCalendarActivity } from './CourtCalendarActivity';

export interface CourtCalendarLocation {
  locationId: string;
  locationShortName: string;
  activities: CourtCalendarActivity[];
}
