import { civilFiledByType } from '@/types/courtlist/jsonTypes';
import { FileDetailsType } from '@/types/shared';
import { AdditionalProperties } from '../../common';
import { ApprDetailType } from '../../shared';

export interface partyAliasType {
  nameTypeCd: string;
  nameTypeDsc: string;
  surnameNm: string;
  firstGivenNm: string;
  secondGivenNm: string;
  thirdGivenNm: string;
  organizationNm: string;
}

export interface partyCounselType {
  counselId: string;
  counselFullName: string;
}

export interface partyType {
  fullName: string;
  roleTypeDescription: string;
  partyId: string;
  lastNm: string;
  givenNm: string;
  orgNm: string;
  roleTypeCd: string;
  leftRightCd: string;
  selfRepresentedYN: string;
  birthDate: string;
  counsel: partyCounselType[];
  aliases: partyAliasType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface PartyRoleType {
  roleTypeCd: string;
  roleTypeDsc: string;
}

export interface PartyDetails {
  fullName: string;
  partyId: string;
  lastNm: string;
  givenNm: string;
  courtParticipantId: string;
  counsel: partyCounselType[];
  representative: Record<string, unknown>[];
  legalRepresentative: Record<string, unknown>[];
  partyRole: PartyRoleType[];
}

export interface civilDocumentIssueType {
  issueTypeDesc: string;
  issueResultCdDesc: string;
  issueNumber: string;
  issueTypeCd: string;
  issueDsc: string;
  concludedYn: string;
  issueResultCd: string;
  issueResultDsc: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilDocumentSupportType {
  actCd: string;
  actDsc: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilDocumentAppearanceType {
  appearanceId: string;
  appearanceDate: string;
  courtAgencyIdentifier: string;
  courtRoom: string;
  appearanceReason: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilDocumentType {
  category: string;
  documentTypeDescription: string;
  nextAppearanceDt: string;
  filedBy: civilFiledByType[];
  issue: civilDocumentIssueType[];
  civilDocumentId: string;
  imageId: string;
  fileSeqNo: string;
  documentTypeCd: string;
  affidavitNo: string;
  swornByNm: string;
  filedDt: string;
  filedByName: string;
  commentTxt: string;
  concludedYn: string;
  lastAppearanceId: string;
  lastAppearanceDt: string;
  lastAppearanceTm: string;
  sealedYN: string;
  DateGranted: string;
  documentSupport: civilDocumentSupportType[];
  appearance: civilDocumentAppearanceType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface civilReferenceDocumentJsonType {
  PartyId: string;
  AppearanceId: string;
  PartyName: string;
  NonPartyName: string;
  ReferenceDocumentInterest: referenceDocumentInterestJsonType[];
  AppearanceDate: string;
  ObjectGuid: string;
  DescriptionText: string;
  EnterDtm: string;
  ReferenceDocumentTypeDsc: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface referenceDocumentInterestJsonType {
  PartyId: string;
  PartyName: string;
  NonPartyName: string;
}

export interface civilHearingRestrictionType {
  hearingRestrictionTypeDsc: string;
  hearingRestrictionId: string;
  adjPartId: string;
  adjFullNm: string;
  hearingRestrictionTypeCd: string;
  applyToNm: string;
  civilDocumentId: string;
  physicalFileId: string;
  adjInitialsTxt: string;
  hearingRestrictionCcn: string;
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}
export interface PersonType {
  givenNm: string;
  lastNm: string;
}

export interface civilApprDetailType extends ApprDetailType {
  documentTypeCd: string;
  documentTypeDsc?: string;
  appearanceResultDsc?: string;
  documentRecCount: string;
}

export interface civilAppearancesType {
  responseCd: string;
  responseMessageTxt: string;
  futureRecCount: string;
  historyRecCount: string;
  apprDetail: civilApprDetailType[];
  additionalProperties: AdditionalProperties;
  additionalProp1: {};
  additionalProp2: {};
  additionalProp3: {};
}

export interface CivilAppearanceDetails {
  physicalFileId: string;
  agencyId: string;
  appearanceId: string;
  courtRoomCd: string;
  fileNumberTxt: string;
  appearanceDt: string;
  appearanceReasonCd: string;
  appearanceReasonDesc: string;
  adjudicator: Record<string, unknown>;
  document: civilDocumentType[];
  binderDocuments: civilDocumentType[];
  appearanceMethod: Record<string, unknown>[];
  courtLevelCd: string;
}

export interface CivilAppearanceDetailParty {
  appearanceId: string;
  party: PartyDetails[];
}

export interface CivilAppearanceDetailDocuments {
  agencyId: string;
  appearanceId: string;
  fileNumberTxt: string;
  courtLevelCd: string;
  document: civilDocumentType[];
}

export interface CivilAppearanceDetailMethods {
  appearanceId: string;
  appearanceMethod: Record<string, unknown>[];
}

export interface civilFileDetailsType extends FileDetailsType {
  physicalFileId: string;
  socTxt: string;
  sealedYN: string;
  leftRoleDsc: string;
  rightRoleDsc: string;
  trialRemarkTxt: string;
  commentToJudgeTxt: string;
  sheriffCommentText: string;
  fileCommentText: string;
  cfcsaFileYN: string;
  party: partyType[];
  document: civilDocumentType[];
  referenceDocument: civilReferenceDocumentJsonType[];
  hearingRestriction: civilHearingRestrictionType[];
}
