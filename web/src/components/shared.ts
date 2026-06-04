import {
  useCriminalDocumentBundleStore,
  useJudicialBinderStore,
  usePDFViewerStore,
  useSnackbarStore,
} from '@/stores';
import { BinderDocument } from '@/types/BinderDocument';
import { civilDocumentType } from '@/types/civil/jsonTypes';
import { CourtRoomsJsonInfoType } from '@/types/common';
import { CourtListAppearance } from '@/types/courtlist';
import { BinderRequest } from '@/types/DocumentBundleRequest';
import { JudicialBinderDocumentRequest } from '@/stores/JudicialBinderStore';
import { CriminalDocumentAppearanceRequest } from '@/stores/CriminalDocumentBundleStore';
import { StoreDocument } from '@/stores/PDFViewerStore';
import {
  CourtDocumentType,
  DataTableHeader,
  DocumentData,
  DocumentRequestType,
} from '@/types/shared';
import { formatDateToDDMMMYYYY } from '@/utils/dateUtils';
import { splunkLog } from '@/utils/utils';
import { v4 as uuidv4 } from 'uuid';
import {
  getCivilDocumentType,
  prepareCivilDocumentData,
} from './documents/DocumentUtils';

type MergedDocumentEntry = {
  documentType: DocumentRequestType;
  documentData: DocumentData;
  groupKeyOne: string;
  groupKeyTwo: string;
  documentName: string;
  physicalFileId: string;
};

type PdfViewerType = 'file' | 'order' | 'criminal-bundle' | 'judicial-binder';

export default {
  getDocumentRequestType(documentType: CourtDocumentType): DocumentRequestType {
    if (documentType === CourtDocumentType.ROP) {
      return DocumentRequestType.ROP;
    }

    if (documentType === CourtDocumentType.CSR) {
      return DocumentRequestType.CourtSummary;
    }

    if (documentType === CourtDocumentType.Transcript) {
      return DocumentRequestType.Transcript;
    }

    if (documentType === CourtDocumentType.Order) {
      return DocumentRequestType.Order;
    }

    return DocumentRequestType.File;
  },

  buildGroupedDocumentMetadata(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): {
    groupKeyOne: string;
    groupKeyTwo: string;
    documentName: string;
  } {
    return {
      groupKeyOne:
        documentData.aslCourtFileNumber ?? documentData.fileNumberText ?? '',
      groupKeyTwo: this.getGroupKeyTwo(documentType, documentData),
      documentName: this.getDocumentDisplayName(documentType, documentData),
    };
  },

  createMergedDocumentEntry(
    documentType: CourtDocumentType,
    documentData: DocumentData,
    physicalFileId = ''
  ): MergedDocumentEntry {
    return {
      documentType: this.getDocumentRequestType(documentType),
      documentData,
      ...this.buildGroupedDocumentMetadata(documentType, documentData),
      physicalFileId,
    };
  },

  getJudicialBinderCourtDocumentType(
    binderDocument: BinderDocument
  ): CourtDocumentType {
    if (binderDocument.documentType === DocumentRequestType.CourtSummary) {
      return CourtDocumentType.CSR;
    }

    if (binderDocument.documentType === DocumentRequestType.Transcript) {
      return CourtDocumentType.Transcript;
    }

    return CourtDocumentType.Civil;
  },

  buildJudicialBinderDocumentData(
    binder: BinderRequest,
    binderDocument: BinderDocument
  ): DocumentData {
    return {
      fileNumberText: binder.fileNumber,
      aslCourtFileNumber: binder.fileNumber,
      fileId: binder.labels['physicalFileId'] ?? '',
      documentDescription:
        binderDocument.fileName ?? String(binderDocument.documentType),
      fileSeqNo: binderDocument.fileSeqNo,
      dateFiled: binderDocument.filedDt,
      orderId: binderDocument.orderId,
      documentId: binderDocument.documentId,
      isCriminal: false,
    };
  },

  createJudicialBinderDocumentEntry(
    binder: BinderRequest,
    binderDocument: BinderDocument
  ): JudicialBinderDocumentRequest {
    const labels = this.compactBinderLabels(binder.labels);
    const documentType =
      this.getJudicialBinderCourtDocumentType(binderDocument);
    const documentData = this.buildJudicialBinderDocumentData(
      binder,
      binderDocument
    );
    const groupedMetadata = this.buildGroupedDocumentMetadata(
      documentType,
      documentData
    );

    return {
      labels,
      fileNumber: binder.fileNumber,
      groupKeyOne: groupedMetadata.groupKeyOne,
      groupKeyTwo: groupedMetadata.groupKeyTwo,
      physicalFileId: labels['physicalFileId'] ?? '',
      participantId: labels['participantId'] ?? '',
      documentName: groupedMetadata.documentName,
      documentId: binderDocument.documentId,
    };
  },

  compactBinderLabels(
    labels: BinderRequest['labels']
  ): Record<string, string> {
    return Object.fromEntries(
      Object.entries(labels).filter(
        ([, value]) => typeof value === 'string' && value !== ''
      )
    );
  },

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

  buildFileViewerUrl(
    type: PdfViewerType,
    sessionId: string,
    params: Record<string, string | string[] | undefined> = {}
  ): string {
    const searchParams = new URLSearchParams();

    searchParams.set('type', type);
    searchParams.set('sessionId', sessionId);

    Object.entries(params).forEach(([key, value]) => {
      if (value === undefined) {
        return;
      }

      if (Array.isArray(value)) {
        if (value.length > 0) {
          searchParams.set(key, value.join(','));
        }

        return;
      }

      if (value) {
        searchParams.set(key, value);
      }
    });

    return `/file-viewer?${searchParams.toString()}`;
  },

  openDocumentsPdf(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): void {
    const fileName = this.generateFileName(
      documentType,
      documentData
    ).replaceAll('/', '_');

    const sessionId = this.addDocumentsToPdfStore([
      this.createMergedDocumentEntry(
        documentType,
        documentData,
        documentData.fileId || ''
      ),
    ]);

    const newWindow = window.open(
      this.buildFileViewerUrl('file', sessionId),
      '_blank'
    );

    this.replaceWindowTitle(newWindow, fileName);
  },

  getGroupKeyTwo(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): string {
    const isCivilFileDocument =
      !documentData.isCriminal &&
      [
        CourtDocumentType.Civil,
        CourtDocumentType.CSR,
        CourtDocumentType.Transcript,
      ].includes(documentType);

    if (isCivilFileDocument) {
      return '';
    }

    return documentData.partyName ?? '';
  },

  getDocumentDisplayName(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): string {
    const isCivilFileDocument =
      !documentData.isCriminal &&
      [
        CourtDocumentType.Civil,
        CourtDocumentType.CSR,
        CourtDocumentType.Transcript,
      ].includes(documentType);

    if (isCivilFileDocument) {
      return [
        documentData.fileSeqNo,
        documentData.documentDescription,
        formatDateToDDMMMYYYY(documentData.dateFiled ?? ''),
      ]
        .filter((value): value is string => !!value)
        .join(' - ');
    }

    return documentData.documentDescription ?? 'Document';
  },

  openMergedDocuments(documents?: MergedDocumentEntry[]): void {
    if (!documents || documents.length === 0) {
      return;
    }

    const sessionId = this.addDocumentsToPdfStore(documents);

    const caseNumbers = Array.from(
      new Set(documents.map((document) => document.groupKeyOne))
    ).join(', ');

    const newWindow = window.open(
      this.buildFileViewerUrl('file', sessionId),
      '_blank'
    );

    this.replaceWindowTitle(newWindow, caseNumbers);
  },

  openCourtListCriminalBundle(
    appearances: CourtListAppearance[],
    categories: string[]
  ): void {
    if (!appearances.length) {
      return;
    }

    const criminalDocumentStore = useCriminalDocumentBundleStore();

    const appearanceRequests: CriminalDocumentAppearanceRequest[] =
      appearances.map((appearance) => ({
        appearance: {
          physicalFileId: appearance.justinNo,
          appearanceId: appearance.appearanceId,
          participantId: appearance.profPartId,
          courtClassCd: appearance.courtClassCd,
        },

        groupKeyOne: appearance.aslCourtFileNumber,
        groupKeyTwo: appearance.accusedNm ?? '',
        documentName: appearance.accusedNm ?? appearance.aslCourtFileNumber,

        fileNumber: appearance.aslCourtFileNumber,
        fullName: appearance.accusedNm,
        physicalFileId: appearance.justinNo,
        participantId: appearance.profPartId,
      }));

    const sessionId = criminalDocumentStore.setPdfItems(appearanceRequests);

    const newWindow = window.open(
      this.buildFileViewerUrl('criminal-bundle', sessionId, {
        category: categories,
      }),
      '_blank'
    );

    const caseNumbers = Array.from(
      new Set(appearances.map((appearance) => appearance.aslCourtFileNumber))
    ).join(', ');

    this.replaceWindowTitle(newWindow, caseNumbers);
  },

  openJudicialBinderDocuments(binders: BinderRequest[]): void {
    const snackbarStore = useSnackbarStore();

    if (!binders.length) {
      snackbarStore.showSnackbar(
        'No viewable documents are available in this binder.',
        'warning',
        'Nothing to open'
      );
      return;
    }

    const judicialBinderStore = useJudicialBinderStore();

    const binderRequests = binders.flatMap((binder) =>
      (binder.documents ?? [])
        .filter((binderDocument) => binderDocument.documentId)
        .map((binderDocument) =>
          this.createJudicialBinderDocumentEntry(binder, binderDocument)
        )
    );

    if (!binderRequests.length) {
      snackbarStore.showSnackbar(
        'No viewable documents are available in this binder.',
        'warning',
        'Nothing to open'
      );
      return;
    }

    judicialBinderStore.clearBundles();

    const sessionId = uuidv4();

    judicialBinderStore.addBundle({
      id: sessionId,
      binders: binderRequests,
    });

    const newWindow = window.open(
      this.buildFileViewerUrl('judicial-binder', sessionId),
      '_blank'
    );

    const caseNumbers = Array.from(
      new Set(binderRequests.map((request) => request.fileNumber))
    ).join(', ');

    this.replaceWindowTitle(newWindow, caseNumbers);
  },

  openOrderDocuments(documentData: DocumentData): void {
    if (!documentData) {
      return;
    }

    const sessionId = this.addDocumentsToPdfStore([
      {
        documentType: DocumentRequestType.Order,
        documentData,
        groupKeyOne: documentData.fileNumberText ?? '',
        groupKeyTwo: '',
        documentName: documentData.documentDescription ?? 'Order',
        physicalFileId: documentData.fileId || '',
      },
    ]);

    const newWindow = window.open(
      this.buildFileViewerUrl('order', sessionId, {
        id: documentData.orderId,
      }),
      '_blank'
    );

    this.replaceWindowTitle(
      newWindow,
      documentData.documentDescription || 'Order'
    );
  },

  addDocumentsToPdfStore(documents: MergedDocumentEntry[]): string {
    const pdfStore = usePDFViewerStore();
    const getEncodedDocumentId = (
      documentType: DocumentRequestType,
      documentData: DocumentData
    ) => {
      if (!documentData.documentId) {
        return documentData.documentId || '';
      }

      if (documentType === DocumentRequestType.Transcript) {
        return documentData.documentId;
      }

      return this.convertToBase64Url(documentData.documentId);
    };

    const storeDocuments: StoreDocument[] = documents.map((document) => ({
      request: {
        type: document.documentType,
        data: {
          partId: document.documentData.partId || '',
          profSeqNo: document.documentData.profSeqNo || '',
          courtLevelCd: document.documentData.courtLevel || '',
          courtClassCd: document.documentData.courtClass || '',
          appearanceId: document.documentData.appearanceId || '',
          documentId: getEncodedDocumentId(
            document.documentType,
            document.documentData
          ),
          fileId: document.documentData.fileId || '',
          isCriminal: document.documentData.isCriminal || false,
          correlationId: uuidv4(),
          courtDivisionCd: document.documentData.courtDivisionCd || '',
          date: document.documentData.date,
          locationId: document.documentData.locationId,
          roomCode: document.documentData.roomCode || '',
          reportType: document.documentData.reportType || '',
          additionsList: document.documentData.additionsList || '',
          orderId: document.documentData.orderId || '',
        },
      },
      groupKeyOne: document.groupKeyOne,
      groupKeyTwo: document.groupKeyTwo,
      documentName: document.documentName,
      physicalFileId: document.physicalFileId,
      documentId: document.documentData.documentId,
      participantId: (
        document.documentData as DocumentData & {
          participantId?: string;
        }
      ).participantId,
    }));

    return pdfStore.setPdfItems(storeDocuments);
  },

  generateFileName(
    documentType: CourtDocumentType,
    documentData: DocumentData
  ): string {
    const locationAbbreviation = (
      documentData.location?.match(/[A-Z]/g) || []
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

      case CourtDocumentType.Transcript:
        return `${locationAbbreviation}-${documentData.courtLevel}-${documentData.fileNumberText}-${documentData.documentDescription}-${documentData.dateFiled}.pdf`;

      default:
        throw new Error(`No file structure for type: ${documentType}`);
    }
  },

  openRequestedTab(
    url: string,
    fileName: string,
    correlationId: string
  ): Window | null {
    const start = new Date();
    const startStr = start.toLocaleString('en-US', {
      timeZone: 'America/Vancouver',
    });

    const startMsg = `Request Tracking - Frontend request to API - CorrelationId: ${correlationId} Start time: ${startStr}`;
    splunkLog(startMsg);

    const windowObjectReference = window.open(url);

    if (windowObjectReference !== null) {
      const end = new Date();
      const endStr = end.toLocaleString('en-US', {
        timeZone: 'America/Vancouver',
      });

      const duration = (end.getTime() - start.getTime()) / 1000;
      const endMsg = `Request Tracking - API response received - CorrelationId: ${correlationId} End time: ${endStr} Duration: ${duration}s`;

      windowObjectReference.onload = () => {
        if (windowObjectReference.document.readyState === 'complete') {
          splunkLog(endMsg);
        }
      };

      this.replaceWindowTitle(windowObjectReference, fileName);
    }

    return windowObjectReference;
  },

  replaceWindowTitle(newWindow: Window | null, title: string): Window | null {
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
    } catch (error) {
      console.error(error);
    }

    return newWindow;
  },

  getBaseCivilDocumentTableHeaders(
    isScheduledCategory = false
  ): DataTableHeader[] {
    return [
      {
        title: 'SEQ',
        key: 'fileSeqNo',
        width: '4rem',
        maxWidth: '4rem',
      },
      {
        title: 'DOCUMENT TYPE',
        key: 'documentTypeDescription',
      },
      {
        title: 'ACT',
        key: 'activity',
      },
      isScheduledCategory
        ? {
            title: 'DATE SCHEDULED',
            key: 'nextAppearanceDt',
            width: '8.5rem',
            maxWidth: '8.5rem',
            value: (item: civilDocumentType) =>
              formatDateToDDMMMYYYY(item.nextAppearanceDt),
            sortRaw: (a: civilDocumentType, b: civilDocumentType) =>
              new Date(a.nextAppearanceDt).getTime() -
              new Date(b.nextAppearanceDt).getTime(),
          }
        : {
            title: 'DATE FILED',
            key: 'filedDt',
            width: '8.5rem',
            maxWidth: '8.5rem',
            value: (item: civilDocumentType) =>
              formatDateToDDMMMYYYY(item.filedDt),
            sortRaw: (a: civilDocumentType, b: civilDocumentType) =>
              new Date(a.filedDt).getTime() - new Date(b.filedDt).getTime(),
          },
      {
        title: 'ORDER MADE',
        key: 'orderMadeDt',
        width: '9.5rem',
        maxWidth: '9.5rem',
        value: (item: civilDocumentType) =>
          formatDateToDDMMMYYYY(item.orderMadeDt),
        sortRaw: (a: civilDocumentType, b: civilDocumentType) =>
          new Date(a.orderMadeDt).getTime() - new Date(b.orderMadeDt).getTime(),
      },
      {
        title: 'FILED / SWORN BY',
        key: 'filedBy',
      },
      {
        title: 'ISSUES',
        key: 'issue',
      },
    ];
  },

  openCivilDocument(
    document: civilDocumentType,
    fileId: string,
    fileNumberTxt: string,
    courtLevel: string,
    agencyId: string,
    locations: CourtRoomsJsonInfoType[]
  ): void {
    const documentData = prepareCivilDocumentData(document);

    documentData.fileId = fileId;
    documentData.fileNumberText ||= fileNumberTxt;
    documentData.courtLevel ||= courtLevel;
    documentData.location ||= locations.find(
      (location) => location.agencyIdentifierCd == agencyId
    )?.name;

    const documentType = getCivilDocumentType(document);
    if (documentType === CourtDocumentType.Transcript) {
      document.category ??= CourtDocumentType[CourtDocumentType.Transcript]; // backwards compatibility with older judicial binders.
      documentData.documentId = document.imageId;
      documentData.orderId = document.transcriptOrderId;
    }
    this.openDocumentsPdf(documentType, documentData);
  },
};
