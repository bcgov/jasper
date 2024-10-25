import {AdjudicatorRestrictionsInfoType} from '../common';
import {criminalFileDetailsType, documentType, countType} from '../criminal/jsonTypes';

export interface participantListInfoType {
    index: string;
    firstName: string;
    lastName: string;
    name: string;
    dob: string;
    partId: string;
    profSeqNo: string;
    charges: chargesInfoType[];         
    status: statusFieldsType[];
    documentsJson: documentType[];
    countsJson: countType[];
    counsel: string;
    counselDesignationFiled: string;
    documents: participantDocumentsInfoType[];
    recordOfProceedings: participantROPInfoType[];
}

export interface appearanceNotesInfoType {
    judgeRec: string;
    appNote: string;
}

export interface chargesInfoType {
    description: string;
    code: string;
}

export interface statusFieldsType {
    key: string;
    abbr: string;
    code: string;
}

export interface criminalFileInformationType {
    participantList: participantListInfoType[];
    adjudicatorRestrictionsInfo: AdjudicatorRestrictionsInfoType[];
    bans: bansInfoType[];
    courtLevel: string;
    courtClass: string;
    detailsData: criminalFileDetailsType;
    fileNumber: string;
}

export interface bansInfoType {
    banParticipant: string;
    banType: string;
    orderDate: string;
    act: string;
    sect: string;
    sub: string;
    description: string;
    comment: string;    
}

export interface criminalCrownInformationInfoType {
    crownInfoFieldName: string;
    crownInfoValue: string;
}

export interface criminalCrownNotesInfoType {
    crownNotes: string;
}

export interface participantDocumentsInfoType {
    date: string;
    documentType: string;
    category: string;
    pages: string;
    pdfAvail: boolean;
    imageId: string;
    status: string;
    statusDate: string;
    isChecked: boolean;
    isEnabled: boolean; 
}

export interface ropRequestsInfoType {
    pdfFileName: string;
    partId: string;
    profSequenceNumber: string;
    courtLevelCode: string;
    courtClassCode: string;
}

export interface participantROPInfoType {    
    documentType: string;
    category: string;
    pages: string;
    pdfAvail: boolean;
    index: string;
    partId: string;
    profSeqNo: string;
    isChecked: boolean;
    isEnabled: boolean; 
} 

export interface criminalAppearancesListType {
    index: string;
    date: string;
    formattedDate: string;
    time: string;
    reason: string;
    reasonDescription: string;
    duration: string;
    location: string;
    room: string;
    firstName: string;
    lastName: string;
    accused: string;
    status: string;
    statusStyle: string;
    presider: string;
    judgeFullName: string;
    appearanceId: string;
    partId: string;
    supplementalEquipment: string;
    securityRestriction: string;
    outOfTownJudge: string;
    profSeqNo: string;
}

export interface criminalAppearanceInfoType {
    fileNo: string;
    courtLevel: string;
    courtClass: string;
    appearanceId: string;
    partId: string;
    supplementalEquipmentTxt: string;
    securityRestrictionTxt: string;
    outOfTownJudgeTxt: string;
    profSeqNo: string;
    date: string;
}

export interface criminalAppearanceDetailsInfoType {
    supplementalEquipment: string;
    outOfTownJudge: string;
    securityRestriction: string; 
}

export interface criminalAppearanceMethodsInfoType {
    role: string;
    method: string;
    instruction: string;
    phoneNumber: string;
}

export interface appearanceChargesInfoType {
    count: string;
    criminalCode: string;
    description: string;
    lastResult: string;
    lastResultDescription: string;
    finding: string;
    findingDescription: string;
}

export interface appearanceMethodDetailsInfoType {
    name: string;
    role: string;
    attendance: string;
    appearance: string;
    partyAppearance: string;
}

export interface appearanceAdditionalInfoType {
    key: string;
    value: string;
}

export interface witnessListInfoType {
    firstName: string;
    lastName: string;
    name: string;           
    type: string;
    required: string;
    agency: string;
    pinCode: string;
    typeCategory: string;
}

export interface witnessCountInfoType {
    witnessCountFieldName: string;    
    witnessCountValue: number;
}

export interface participantSentencesInfoType {
    index: string;
    lastName: string;
    name: string;
    countsJson: countType[];
    counts: countInfoType[];
    countsDisable: boolean;
    orderMade: countInfoType[];
    orderMadeDisable: boolean;
    judgesRecommendation: countInfoType[];
    recommendationDisable: boolean;
}

export interface countInfoType {

    date: string;
    formattedDate: string;
    finding: string;
    findingDsc: string;                                   
    chargeIssueCd: string[];
    chargeIssueDsc: string[];
    chargeIssueDscFull: string[];
    count: string;
    sentenceDispositionType: string[];
    sentenceDsc: string[];
    term: string[];
    amount: string[];
    dueDateUntil: string[];
    effectiveDate: string[];
    orderMade: string[];
    judgeRecommendation: string[];
    lenCharge: number;
    len: number;
}

export interface participantSentencesDetailsInfoType {
    participantSentences: participantSentencesInfoType[];
    selectedParticipant: number;
}

export interface showSectionsInfoType {
    caseDetails: boolean;
    futureAppearances: boolean;
    pastAppearances: boolean; 
    witnesses: boolean;
    documents: boolean;
    sentenceOrderDetails: boolean;
}

export interface fileSearchCriminalInfoType {
    participants: participantInfoType[];
    fileId: string;
    level: string;
    fileNumber: string;
    nextAppearance: string;
    isChecked: boolean;
    today: boolean;
}

export interface participantInfoType {
    name: string;
    charge: string;
}

export interface initiatingDocument {
    imageId: string;
    issueDate: string;
}