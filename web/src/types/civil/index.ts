import {AdjudicatorRestrictionsInfoType} from '../common';
import {civilFileDetailsType} from '../civil/jsonTypes';

export interface partiesInfoType {
    partyId: string;
    role: string;
    counsel: string[];
    leftRight: string;
    firstName: string;
    lastName: string;
    name: string;
    id: string;
    index: number;
}

export interface actType {
    code: string;
    description: string;
}

export interface documentsInfoType {
    index?: string;
    seq: string;
    documentType: string;
    concluded: string;
    nextAppearanceDate: string; 
    category: string;
    swornBy: string;
    affNo: string;
    act: actType[];           
    sealed: boolean;
    documentId: string;            
    pdfAvail: boolean; 
    dateFiled: string;
    issues: string[];
    comment: string;
    orderMadeDate?: string;
    filedByName: string[];
    dateGranted: string;
    isChecked: boolean;
    isEnabled: boolean;
}

export interface referenceDocumentsInfoType {
    partyId: string[];
    appearanceId: string;
    partyName: string[];
    nonPartyName: string[];
    appearanceDate: string;
    descriptionText: string;
    enterDtm?: string;
    referenceDocumentTypeDsc: string;
    objectGuid: string;
    isChecked: boolean;
    isEnabled: boolean;
}

export interface summaryDocumentsInfoType {
    index?: string;
    documentType: string;
    appearanceDate: string;
    appearanceId: string;
    pdfAvail: boolean;
    isChecked: boolean;
    isEnabled: boolean;    
}

export interface civilFileInformationType {
    adjudicatorRestrictionsInfo: AdjudicatorRestrictionsInfoType[];
    fileNumber: string;
    detailsData: civilFileDetailsType;
    leftPartiesInfo: partiesInfoType[];
    rightPartiesInfo: partiesInfoType[];
    isSealed: boolean;
    documentsInfo: documentsInfoType[];
    referenceDocumentInfo: referenceDocumentsInfoType[];
    summaryDocumentsInfo: summaryDocumentsInfoType[];
    categories: string[];
    providedDocumentCategories: string[];
}

export interface csrRequestsInfoType {
    pdfFileName: string;
    appearanceId: string;
}

export interface civilAppearanceInfoType {
    fileNo: string;
    appearanceId: string;
    supplementalEquipmentTxt: string;
    securityRestrictionTxt: string;
    outOfTownJudgeTxt: string;
    date: string;
}

export interface civilAppearanceDetailsInfoType {
    supplementalEquipment: string;
    outOfTownJudge: string;
    securityRestriction: string; 
}

export interface civilIssueType {
    issue: string;
    result: string;
    resultDsc: string;
}

export interface appearanceDocumentsType {
    seq: string;
    documentType: string;
    docTypeCd: string;
    id: string;
    pdfAvail: boolean;
    act: actType[];            
    sealed: boolean;
    dateFiled: string;
    result: string;
    resultDescription: string;
    issues: civilIssueType[];
    index: string;
}

export interface currentCounselType {
    name: string; 
    info: string;
}

export interface representativeType {
    name: string; 
    info: string;
}

export interface legalRepresentativeType {
    name: string; 
    type: string;
}

export interface appearancePartiesType {
    firstName: string;
    lastName: string;
    name: string;
    info: string;
    currentCounsel: currentCounselType[];
    representative: representativeType[];
    legalRepresentative: legalRepresentativeType[];
    role: string[];
}

export interface appearanceMethodsType {
    role: string;
    method: string;
}

export interface appearanceAdditionalInfoType {
    key: string;
    value: string;
}

export interface civilAppearancesListType {
    index: string;
    date: string;
    formattedDate: string;
    documentType: string;
    result: string;
    resultDescription: string;
    time: string;
    reason: string;
    reasonDescription: string;
    duration: string;
    location: string;
    room: string;
    status: string;
    statusStyle: string;
    presider: string;
    judgeFullName: string;
    appearanceId: string;
    supplementalEquipment: string;
    securityRestriction: string;
    outOfTownJudge: string;
}

export interface civilNotesType {
    notesFieldName: string;
    notesValue: string;
}

export interface civilShowSectionsType {
    caseDetails: boolean;
    futureAppearances: boolean;
    pastAppearances: boolean;
}

export interface fileSearchCivilInfoType {
    parties: string[];
    fileId: string;
    fileNumber?: string;
    nextAppearance: string;
    level?: string;
    isChecked: boolean;
}