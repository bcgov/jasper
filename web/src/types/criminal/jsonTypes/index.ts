import {AdditionalProperties} from '../../common';

export interface countSentenceType {
    judgesRecommendation: string;
    sentenceTypeDesc: string;
    sntpCd: string;
    sentTermPeriodQty: string;
    sentTermCd: string;
    sentSubtermPeriodQty: string;
    sentSubtermCd: string;
    sentTertiaryTermPeriodQty: string;
    sentTertiaryTermCd: string;
    sentIntermittentYn: string;
    sentMonetaryAmt: string;
    sentDueTtpDt: string;
    sentEffectiveDt: string;
    sentDetailTxt: string;
    sentYcjaAdultYouthCd: string;
    sentCustodySecureYn: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface countType {
    partId: string;
    appearanceDate: string;
    findingDsc: string;
    appcId: string;
    appearanceReason: string;
    sentence: countSentenceType[];
    countNumber: string;
    appearanceResult: string;
    finding: string;
    sectionTxt: string;
    sectionDscTxt: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
    }

export interface documentType {
    partId: string;
    category: string;
    documentTypeDescription: string;
    hasFutureAppearance: boolean;
    docmClassification: string;
    docmId: string;
    issueDate: string;
    docmFormId: string;
    docmFormDsc: string;
    docmDispositionDsc: string;
    docmDispositionDate: string;
    imageId: string;
    documentPageCount: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface chargeType {    
    sectionTxt: string;
    sectionDscTxt: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};        
}

export interface banType {    
    partId: string;
    banTypeCd: string;
    banTypeDescription: string;
    banTypeAct: string;
    banTypeSection: string;
    banTypeSubSection: string;
    banStatuteId: string;
    banCommentText: string;
    banOrderedDate: string;
    banSeqNo: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};        
}

export interface witnessType {
    fullName: string;
    witnessTypeDsc: string;
    agencyDsc: string;
    agencyCd: string;
    partId: string;
    lastNm: string;
    givenNm: string;
    witnessTypeCd: string;
    roleTypeCd: string;
    requiredYN: string;
    confidentialYN: string;
    pinCodeTxt: string;
    agencyId: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface criminalTrialRemarkType {    
    commentTxt: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface criminalParticipantType {
    fullName: string;
    document: documentType[];            
    hideJustinCounsel: boolean;
    count: countType[];           
    ban: banType[];
    partId: string;
    profSeqNo: string;
    lastNm: string;
    givenNm: string;
    orgNm: string;
    warrantYN: string;
    inCustodyYN: string;
    interpreterYN: string;
    detainedYN: string;
    birthDt: string;
    counselRrepId: string;
    counselPartId: string;
    counselLastNm: string;
    counselGivenNm: string;
    counselRelatedRepTypeCd: string;
    counselEnteredDt: string;
    designatedCounselYN: string;
    charge: chargeType[];
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface criminalHearingRestrictionType {
    hearingRestrictionTypeDsc: string;
    hearingRestrictionId: string;
    adjPartId: string;
    adjFullNm: string;
    hearingRestrictionTypeCd: string;
    partId: string;
    profSeqNo: string;
    partNm: string;
    adjInitialsTxt: string;
    justinNo: string;
    hearingRestrictionCcn: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface crownType {
    partId: string;
    assigned: boolean;
    fullName: string;
    lastNm: string;
    givenNm: string;
}

export interface criminalApprDetailType {
    historyYN: string;
    appearanceId: string;
    appearanceDt: string;
    appearanceTm: string;
    appearanceReasonCd: string;
    appearanceReasonDsc?: string;
    courtLocation?: string;
    courtAgencyId: string;
    courtRoomCd: string;
    judgeFullNm: string;
    judgeInitials: string;
    counselFullNm: string;
    estimatedTimeHour: string;
    estimatedTimeMin: string;
    partOfTrialYN: string;
    appearanceStatusCd: string;
    partId: string;
    profSeqNo: string;
    lastNm: string;
    givenNm: string;
    orgNm: string;
    appearanceResultCd: string;
    appearanceCcn: string;
    supplementalEquipmentTxt: string;
    securityRestrictionTxt: string;
    outOfTownJudgeTxt: string;
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface criminalAppearancesType {
    responseCd: string;
    responseMessageTxt: string;
    futureRecCount: string;
    historyRecCount: string;
    apprDetail: criminalApprDetailType [];
    additionalProperties: AdditionalProperties;
    additionalProp1: {};
    additionalProp2: {};
    additionalProp3: {};
}

export interface criminalFileDetailsType {
    
    responseCd: string;
    responseMessageTxt: string;
    justinNo: string;
    fileNumberTxt: string;
    homeLocationAgenId: string;
    homeLocationAgencyName: string;
    homeLocationAgencyCode: string;
    homeLocationRegionName: string;
    courtLevelCd: string;
    courtLevelDescription: string;
    courtClassCd: string;
    courtClassDescription: string;
    activityClassCd: string;
    activityClassDesc: string;
    currentEstimateLenQty: string;
    currentEstimateLenUnit: string;
    initialEstimateLenQty: string;
    initialEstimateLenUnit: string;
    trialStartDt: string;
    mdocSubCategoryDsc: string;
    mdocCcn: string;
    assignedPartNm: string;
    approvedByAgencyCd: string;
    approvedByPartNm: string;
    approvalCrownAgencyTypeCd: string;
    crownEstimateLenQty: number;
    crownEstimateLenDsc: string;
    crownEstimateLenUnit: string;
    caseAgeDays: string;
    indictableYN: string;
    complexityTypeCd: string;
    assignmentTypeDsc: string;
    trialRemark: criminalTrialRemarkType[];
    participant: criminalParticipantType[];
    witness: witnessType[];
    crown: crownType[];
    hearingRestriction: criminalHearingRestrictionType[];
    appearances: criminalAppearancesType;
}