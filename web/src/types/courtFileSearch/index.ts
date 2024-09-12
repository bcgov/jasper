// Intefaces
export interface CourtFileSearchCriteria {
  isCriminal: boolean;
  selectedFileNoOrParty: string;
  fileNumber?: string;
  prefix?: string;
  seqNum?: string;
  typeRef?: string;
  surname?: string;
  givenName?: string;
  org?: string;
  class?: string;
  location: string;
}

// Enums
export enum CourtClassEnum {
  Adult = "A",
  Family = "F",
  SmallClaims = "C",
  Youth = "Y"
}

export enum SearchModeEnum {
  FileNo = "FILENO",
  PartName = "PARTNAME",
}