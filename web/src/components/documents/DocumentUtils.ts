import { beautifyDate } from '@/filters';
import { useCivilFileStore, useCriminalFileStore } from '@/stores';
import { civilDocumentType } from '@/types/civil/jsonTypes';
import { documentType } from '@/types/criminal/jsonTypes';
import { CourtDocumentType, DocumentData } from '@/types/shared';

const ROP = 'rop';
const CSR = 'CSR';

export const prepareCriminalDocumentData = (data) => {
  const criminalFileStore = useCriminalFileStore();
  const documentData: DocumentData = {
    courtClass:
      criminalFileStore.criminalFileInformation.detailsData.courtClassCd,
    courtLevel:
      criminalFileStore.criminalFileInformation.detailsData.courtLevelCd,
    dateFiled: beautifyDate(data.date),
    documentId: data.imageId,
    documentDescription:
      data.category?.toLowerCase() === ROP
        ? 'Record of Proceedings'
        : data.documentTypeDescription,
    fileId: criminalFileStore.criminalFileInformation.fileNumber,
    fileNumberText:
      criminalFileStore.criminalFileInformation.detailsData.fileNumberTxt,
    partId: data.partId,
    profSeqNo: data.profSeqNo,
    location:
      criminalFileStore.criminalFileInformation.detailsData
        .homeLocationAgencyName,
    isCriminal: true,
  };
  return documentData;
};

export const prepareCivilDocumentData = (data) => {
  const civilFileStore = useCivilFileStore();
  const documentData: DocumentData = {
    appearanceDate: beautifyDate(data.lastAppearanceDt),
    appearanceId:
      data.appearanceId ?? data.civilDocumentId,
    dateFiled: beautifyDate(data.filedDt),
    documentDescription: data.documentTypeCd,
    documentId: data.civilDocumentId,
    fileId: civilFileStore.civilFileInformation.fileNumber,
    fileNumberText: civilFileStore.civilFileInformation.detailsData.fileNumberTxt,
    courtClass: civilFileStore.civilFileInformation.detailsData.courtClassCd,
    courtLevel: civilFileStore.civilFileInformation.detailsData.courtLevelCd,
    location:
      civilFileStore.civilFileInformation.detailsData.homeLocationAgencyName,
    isCriminal: false,
  };
  return documentData;
};

export const getCriminalDocumentType = (
  data: documentType
): CourtDocumentType => {
  return data.category?.toLowerCase() === ROP
    ? CourtDocumentType.ROP
    : CourtDocumentType.Criminal;
};

export const getCivilDocumentType = (
  data: civilDocumentType
): CourtDocumentType => {
  return data.documentTypeCd == CSR
    ? CourtDocumentType.CSR
    : CourtDocumentType.Civil;
};
