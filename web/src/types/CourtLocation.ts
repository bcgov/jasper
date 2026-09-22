export interface PhoneInfo {
  phone: string;
  notes: string;
}

export interface EmailInfo {
  email: string;
  notes: string;
}

export interface ProbationOffice {
  name: string;
  address1: string;
  address2: string;
  staffingNotes: string;
}

export interface AdultProbationOffice extends ProbationOffice {
  phones: PhoneInfo[];
  tollFreePhone?: PhoneInfo;
}

export interface YouthProbationOffice extends ProbationOffice {
  city: string;
  contactName: string;
  phone?: PhoneInfo;
}

export interface CourtLocation {
  id: string;
  code: string;
  name: string;
  jasperName: string;
  path: string;
  address1: string;
  address2: string;
  city: string;
  isStaffed: boolean;
  iarSchedule: string;
  fxdSchedule: string;
  jcmPhones: PhoneInfo[];
  emails: EmailInfo[];
  adultProbationOffice?: AdultProbationOffice;
  youthProbationOffice?: YouthProbationOffice;
}
