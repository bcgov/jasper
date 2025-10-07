import { useBundleStore, usePDFViewerStore } from '@/stores';
import { AppearanceDocumentRequest } from '@/types/AppearanceDocumentRequest';
import { CourtListAppearance } from '@/types/courtlist';
import { DocumentBundleRequest } from '@/types/DocumentBundleRequest';
import {
  CourtDocumentType,
  DocumentData,
  DocumentRequestType,
} from '@/types/shared';
import { splunkLog } from '@/utils/utils';
import { v4 as uuidv4 } from 'uuid';

export default {
  convertToBase64Url(inputText: string): string {
    const base64 = btoa(inputText);
    return base64.replace(/[+/=]/g, (char) => {
      switch (char) {
        case '+':
          return '-';
        case '/':
          return '_';
        case '=':
          return '';
        default:
          return char;
      }
    });
  },
  renderDocumentUrl(
    documentType: CourtDocumentType,
    documentData: DocumentData,
    correlationId: string,
    fileName: string
  ): string {
    const isCriminal = documentType == CourtDocumentType.Criminal;
    const documentId = documentData.documentId
      ? this.convertToBase64Url(documentData.documentId)
      : documentData.documentId;

    switch (documentType) {
      case CourtDocumentType.CSR:
        return `${import.meta.env.BASE_URL}api/files/civil/court-summary-report/${
          documentData.appearanceId
        }/${encodeURIComponent(fileName)}?vcCivilFileId=${documentData.fileId}`;
      case CourtDocumentType.ROP:
        return `${
          import.meta.env.BASE_URL
        }api/files/criminal/record-of-proceedings/${
          documentData.partId
        }/${encodeURIComponent(fileName)}?profSequenceNumber=${
          documentData.profSeqNo
        }&courtLevelCode=${documentData.courtLevel}&courtClassCode=${
          documentData.courtClass
        }`;
      default:
        return `${
          import.meta.env.BASE_URL
        }api/files/document/${documentId}/${encodeURIComponent(
          fileName
        )}?isCriminal=${isCriminal}&fileId=${
          documentData.fileId
        }&CorrelationId=${correlationId}`;
    }
  },
  openDocumentsPdf(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): void {
    const fileName = this.generateFileName(documentType, documentData).replace(
      /\//g,
      '_'
    );
    let type = DocumentRequestType.File;
    if (documentType === CourtDocumentType.ROP) {
      type = DocumentRequestType.ROP;
    } else if (documentType === CourtDocumentType.CSR) {
      type = DocumentRequestType.CourtSummary;
    }
    this.addDocumentsToPdfStore([
      {
        documentType: type,
        documentData: documentData,
        groupKeyOne: documentData.fileNumberText ?? '',
        groupKeyTwo: documentData.partyName ?? '',
        documentName: fileName,
      },
    ]);

    const newWindow = window.open('/file-viewer?type=file', '_blank');

    this.replaceWindowTitle(newWindow, fileName);
  },
  openMergedDocuments(
    documents?: {
      documentType: DocumentRequestType;
      documentData: DocumentData;
      groupKeyOne: string;
      groupKeyTwo: string;
      documentName: string;
    }[]
  ): void {
    if (!documents || documents.length === 0) return;
    this.addDocumentsToPdfStore(documents);

    const caseNumbers = Array.from(
      new Set(documents.map((d) => d.groupKeyOne))
    ).join(', ');
    const newWindow = window.open('/file-viewer?type=file', '_blank');

    this.replaceWindowTitle(newWindow, caseNumbers);
  },
  openCourtListKeyDocuments(appearances: CourtListAppearance[]): void {
    if (!appearances.length) return;
    const bundleStore = useBundleStore();
    const appearanceRequests = appearances.map((app) => ({
      appearance: {
        physicalFileId: app.justinNo,
        appearanceId: app.appearanceId,
        participantId: app.profPartId,
        courtClassCd: app.courtClassCd,
      } as AppearanceDocumentRequest,
      fileNumber: app.courtFileNumber,
      fullName: app.accusedNm,
    }));
    const bundleRequest = {
      appearances: appearanceRequests.map((app) => app.appearance),
    } as DocumentBundleRequest;
    bundleStore.request = bundleRequest;
    bundleStore.appearanceRequests = appearanceRequests;

    const newWindow = window.open('/file-viewer?type=bundle', '_blank');
    const caseNumbers = Array.from(
      new Set(appearances.map((d) => d.courtFileNumber))
    ).join(', ');
    this.replaceWindowTitle(newWindow, caseNumbers);
  },
  addDocumentsToPdfStore(
    documents: {
      documentType: DocumentRequestType;
      documentData: DocumentData;
      groupKeyOne: string;
      groupKeyTwo: string;
      documentName: string;
    }[]
  ): void {
    const pdfStore = usePDFViewerStore();
    pdfStore.clearDocuments();
    documents.forEach((doc) => {
      pdfStore.addDocument({
        request: {
          type: doc.documentType,
          data: {
            partId: doc.documentData.partId || '',
            profSeqNo: doc.documentData.profSeqNo || '',
            courtLevelCd: doc.documentData.courtLevel || '',
            courtClassCd: doc.documentData.courtClass || '',
            appearanceId: doc.documentData.appearanceId || '',
            documentId: doc.documentData.documentId
              ? this.convertToBase64Url(doc.documentData.documentId)
              : doc.documentData.documentId || '',
            fileId: doc.documentData.fileId || '',
            isCriminal: doc.documentData.isCriminal || false,
            correlationId: uuidv4(),
            courtDivisionCd: doc.documentData.courtDivisionCd || '',
            date: doc.documentData.date,
            locationId: doc.documentData.locationId,
            roomCode: doc.documentData.roomCode || '',
            reportType: doc.documentData.reportType || '',
            additionsList: doc.documentData.additionsList || '',
          },
        },
        groupKeyOne: doc.groupKeyOne,
        groupKeyTwo: doc.groupKeyTwo,
        documentName: doc.documentName,
      });
    });
  },
  generateFileName(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): string {
    const locationAbbreviation = (
      documentData.location.match(/[A-Z]/g) || []
    ).join('');
    switch (documentType) {
      case CourtDocumentType.Civil:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.dateFiled}-${documentData.documentId}.pdf`;
      case CourtDocumentType.ProvidedCivil:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.appearanceDate}-${documentData.partyName}.pdf`;
      case CourtDocumentType.CSR:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.appearanceDate}.pdf`;
      case CourtDocumentType.Criminal:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.courtClass}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.dateFiled}-${documentData.documentId}.pdf`;
      case CourtDocumentType.ROP:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.courtClass}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.partId}.pdf`;
      case CourtDocumentType.CivilZip:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-documents.zip`;
      case CourtDocumentType.CriminalZip:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.courtClass}-${documentData.fileNumberText}-documents.zip`;
      default:
        throw Error(`No file structure for type: ${documentType}`);
    }
  },

  openRequestedTab(url, fileName, correlationId) {
    const start = new Date();
    const startStr = start.toLocaleString('en-US', {
      timeZone: 'America/Vancouver',
    });
    const startMsg = `Request Tracking - Frontend request to API - CorrelationId: ${correlationId} Start time: ${startStr}`;
    //console.log(startMsg);
    splunkLog(startMsg);

    const windowObjectReference = window.open(url);
    if (windowObjectReference !== null) {
      const end = new Date();
      const endStr = start.toLocaleString('en-US', {
        timeZone: 'America/Vancouver',
      });

      const duration = (end.getTime() - start.getTime()) / 1000;
      const endMsg = `Request Tracking - API response received - CorrelationId: ${correlationId} End time: ${endStr} Duration: ${duration}s`;

      // eslint-disable-next-line
      windowObjectReference.onload = (event) => {
        if (windowObjectReference.document.readyState === 'complete') {
          //console.log(endMsg);
          splunkLog(endMsg);
        }
      };
      this.replaceWindowTitle(windowObjectReference, fileName);
    }
  },

  replaceWindowTitle(newWindow: Window | null, title: string) {
    if (newWindow === null) {
      return null;
    }
    try {
      newWindow.addEventListener('load', function () {
        setTimeout(function () {
          newWindow.document.title = title;
        }, 1000);
        setTimeout(function () {
          newWindow.document.title = title;
        }, 3000);
        setTimeout(function () {
          newWindow.document.title = title;
        }, 5000);
      });
    } catch (e) {
      console.error(e);
    }
    return newWindow;
  },
};
