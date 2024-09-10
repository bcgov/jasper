export interface CourtFileSearchCriteria {
  division: string;
  isCriminal: boolean;
  isCivil: boolean;
  isOther: boolean;

  selectedFileNoOrParty: string;
  fileNumber?: string;
  prefix?: string;
  seqNum?: string;
  typeRef?: string;
  surname?: string;
  givenName?: string;
  org?: string;
  class: string;

  selectedType?: string;
  courtOfAppeal?: string;
  otrOrSealed?: string;
  styleOfCause?: string;
  judge?: string;
  room?: string;
  proceedingDates: string[];

  registry: string;

  startDate?: string;
  endDate?: string;
}
