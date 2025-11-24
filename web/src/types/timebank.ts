export interface VacationSummaryBucket {
  jan1Entitlement: number;
  jan1Adjustment: number;
  jan1Total: number;
  otherAdjustment: number;
  totalAdjustment: number;
  otherEntitlement: number;
  total: number;
  remaining: number;
}

export interface VacationSummaryBucketCarryOver {
  jan1CarryOver: number;
  jan1Adjustment: number;
  jan1Total: number;
  otherAdjustment: number;
  totalAdjustment: number;
  otherCarryOver: number;
  total: number;
  remaining: number;
}

export interface TimebankLineItem {
  judiciaryPersonId: number;
  period: number;
  date?: string;
  effectiveDate?: string;
  expiryDate?: string;
  timebankEntryTypeCd?: string;
  days: number;
  hours: number;
  annualDays: number;
  annualHours: number;
  isActualEntry: boolean;
  activityCd?: string;
  activityClassCd?: string;
  isHalfDay: boolean;
  isPeriodEntitlementEntry: boolean;
  isRolloverEntry: boolean;
  isManualEntry: boolean;
  vacationTypeCd?: string;
  comments?: string;
  activitySummary?: string;
  updatedOn?: string;
  updatedBy?: string;
}

export interface TbSummaryFlag {
  amount?: number;
  reason?: string;
  shortDescription?: string;
  description?: string;
}

export interface VacationSummary {
  timebankEntryTypeCd?: string;
  period: number;
  entitlementCalcType?: string;
  isHours: boolean;
  lineItems?: TimebankLineItem[];
  flags?: TbSummaryFlag[];
  vacationScheduled: number;
  regular?: VacationSummaryBucket;
  regularCarryOver?: VacationSummaryBucketCarryOver;
  extraDuties?: VacationSummaryBucket;
  extraDutiesCarryOver?: VacationSummaryBucketCarryOver;
  total: number;
  totalRemaining: number;
}

export interface TimebankSummary {
  judiciaryPersonId: number;
  firstNm?: string;
  surnameNm?: string;
  judiciaryTypeCd?: string;
  judiciaryPositionCd?: string;
  judiciaryStatusCd?: string;
  judiciaryInactiveReasonCd?: string;
  entitlementCalcType?: string;
  seniorDayYn?: string;
  locationId: number;
  period: number;
  scheduleCompleteYn?: string;
  vacation?: VacationSummary;
}

export interface VacationSummaryItem {
  desc: string;
  amount: number;
}

export interface VacationPayout {
  judiciaryPersonId: number;
  period: number;
  effectiveDate: string;
  entitlementCalcType: string | null;
  vacationCurrent: number;
  vacationBanked: number;
  extraDutyCurrent: number;
  extraDutyBanked: number;
  vacationUsed: number;
  vacationCurrentRemaining: number;
  vacationBankedRemaining: number;
  extraDutyCurrentRemaining: number;
  extraDutyBankedRemaining: number;
  rate: number;
  totalCurrent: number;
  totalBanked: number;
  totalPayout: number;
}
