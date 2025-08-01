import { csrRequestsInfoType } from '../civil';
import { ropRequestsInfoType } from '../criminal';

export interface InputNamesType {
  lastName: string;
  givenName: string;
}

export interface DurationType {
  hr: string;
  min: string;
}

export interface IconStyleType {
  icon: string;
  desc: string;
}

export interface IconInfoType {
  info: string;
  desc: string;
}

export interface AdditionalProperties {
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface AdjudicatorRestrictionsInfoType {
  adjRestriction: string;
  adjudicator: string;
  fullName: string;
  status: string;
  appliesTo: string;
}

export interface DocumentRequestsInfoType {
  isCriminal: boolean;
  pdfFileName: string;
  base64UrlEncodedDocumentId: string;
  fileId: string;
}

export interface ArchiveInfoType {
  zipName: string;
  vcCivilFileId?: string;
  csrRequests: csrRequestsInfoType[];
  documentRequests: DocumentRequestsInfoType[];
  ropRequests: ropRequestsInfoType[];
}

export interface CourtRoomsJsonInfoType {
  name: string;
  code: string;
  locationId: string;
  active: boolean;
  courtRooms: CourtRoomsInfo[];
  infoLink: string;
}

export interface CourtRoomsInfo {
  room: string;
  locationId: string;
  type: string;
}

export interface UserInfo {
  userType: string;
  enableArchive: boolean;
  role: string;
  subRole: string;
  isSupremeUser: string;
  agencyCode: string;
  userId: string;
  judgeId: string;
}

export interface LookupCode {
  codeType: string;
  code: string;
  shortDesc: string;
  longDesc: string;
}

export interface KeyValueInfo {
  key: string;
  value: string;
}

export enum DivisionEnum {
  R = 'R',
  I = 'I',
}
// Can't seem to import this type from Vuetify yet, so defining it here until they're made available
export enum Anchor {
  Start = 'start',
  End = 'end',
  Top = 'top',
  Bottom = 'bottom',
}

export enum RoleTypeEnum {
  CHD = 'CHD',
  COF = 'COF',
}

export enum CourtClassEnum {
  A = 0,
  Y = 1,
  T = 2,
  F = 3,
  C = 4,
  M = 5,
  L = 6,
}

export enum FileMarkerEnum {
  ADJ = 0,
  CNT,
  CPA,
  CSO,
  DO,
  IC,
  INT,
  LOCT,
  OTH,
  W,
}
