export interface JudicialCalendarActivity {
  courtActivityId: number | null;
  activityCode: string;
  activityDescription: string;
  activityDisplayCode: string;
  activityClassCode: string;
  activityClassDescription: string;
  judiciaryTypeCode: string;
  locationId: number | null;
  locationName: string;
  fromLocationId: number | null;
  fromLocationName: string;
  courtRoomCode: string;
  courtSittingCode: string;
  isVideo: boolean | null;
  isJJ: boolean | null;
  isPCJ: boolean | null;
  isJP: boolean | null;
  isOther: boolean | null;
  isIAR: boolean | null;
  isWithinLookaheadWindow: boolean | null;
}
