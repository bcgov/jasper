import {AdditionalProperties} from '../../common';
import { civilFiledByType } from '@/types/courtlist/jsonTypes';

export interface partyCounselType {
    counselId: string;
    fullNm: string;
    phoneNumberTxt: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
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
    counsel: partyCounselType[];
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
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

export interface civilApprDetailType {
    historyYN: string;
    appearanceId: string;
    appearanceDt: string;
    appearanceTm: string;
    appearanceReasonCd: string;
    courtAgencyId: string;
    courtRoomCd: string;
    judgeFullNm: string;
    judgeInitials: string;
    estimatedTimeHour: string;
    estimatedTimeMin: string;
    partOfTrialYN: string;
    appearanceStatusCd: string;
    appearanceResultCd: string;
    appearanceCcn: string;
    documentTypeCd: string;
    documentTypeDsc?: string;
    appearanceResultDsc?: string;
    appearanceReasonDsc?: string;
    courtLocation?: string;
    documentRecCount: string;
    supplementalEquipmentTxt: string;
    securityRestrictionTxt: string;
    outOfTownJudgeTxt: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
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

export interface civilFileDetailsType {    
    responseCd: string;
    responseMessageTxt: string;
    physicalFileId: string;
    fileNumberTxt: string;
    socTxt: string;
    courtClassCd: string;
    courtClassDescription: string;
    courtLevelCd: string;
    courtLevelDescription: string;
    activityClassCd: string;
    activityClassDesc: string;
    sealedYN: string;
    leftRoleDsc: string;
    rightRoleDsc: string;
    homeLocationAgenId: string;
    homeLocationAgencyName: string;
    homeLocationAgencyCode: string;
    homeLocationRegionName: string;
    trialRemarkTxt: string;
    commentToJudgeTxt: string;
    sheriffCommentText: string;
    fileCommentText: string;
    party: partyType[];
    document: civilDocumentType[];
    referenceDocument: civilReferenceDocumentJsonType[];
    hearingRestriction: civilHearingRestrictionType[];
    appearances: civilAppearancesType;
}
