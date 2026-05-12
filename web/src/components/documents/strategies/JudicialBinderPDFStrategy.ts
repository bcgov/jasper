import { OutlineItem, PDFViewerStrategy } from './PDFViewerTypes';
import { BinderService } from '@/services';
import { useJudicialBinderStore } from '@/stores';
import { ApiResponse } from '@/types/ApiResponse';
import { BinderDocument } from '@/types/BinderDocument';
import { BinderDocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { DocumentBundleResponse } from '@/types/DocumentBundleResponse';
import { BinderDocumentRequest } from '@/types/BinderDocumentRequest';
import { inject } from 'vue';

export type JudicialBinderRawData = {
  binder: BinderDocumentRequest;
  fileNumber: string;
}[];

export type BinderLabelContext = Record<string, string>;

export class JudicialBinderPDFStrategy implements PDFViewerStrategy<
  JudicialBinderRawData,
  BinderLabelContext[],
  ApiResponse<DocumentBundleResponse>
> {
  private readonly binderStore = useJudicialBinderStore();
  private readonly binderService: BinderService;
  private count = 0;

  constructor() {
    const service = inject<BinderService>('binderService');
    if (!service) {
      throw new Error('BinderService is not available!');
    }
    this.binderService = service;
  }

  hasData(): boolean {
    const request = this.binderStore.getRequests;
    return !!(
      request &&
      'binders' in request &&
      (request as BinderDocumentBundleRequest).binders?.length > 0
    );
  }

  getRawData(): JudicialBinderRawData {
    const request = this.binderStore.getRequests as BinderDocumentBundleRequest;
    if (!request.binders) {
      return [];
    }

    // Return the raw binder requests with file numbers
    return request.binders.map((binder) => ({
      binder,
      fileNumber: binder.physicalFileId,
    }));
  }

  processDataForAPI(rawData: JudicialBinderRawData): BinderLabelContext[] {
    // Convert to label contexts for API compatibility
    return rawData.map((item) => ({
      physicalFileId: item.binder.physicalFileId,
      courtClassCd: item.binder.courtClassCd,
      ...(item.binder.judgeId ? { judgeId: item.binder.judgeId } : {}),
    }));
  }

  /**
   * Retrieves existing judicial binder(s) with all bundled documents.
   * This is a read-only operation for civil judicial binders.
   * @param processedData
   * @returns pdf including all retrieved binders, or 404 if no binders are found.
   */
  async getPdf(
    processedData: BinderLabelContext[]
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    const urlParams = new URLSearchParams(globalThis.location.search);
    const documentCategories = urlParams.get('category')?.split(',') || [];
    return await this.binderService.viewBinderPDF(
      processedData,
      documentCategories
    );
  }

  /**
   * For judicial binders, generatePDF delegates to getPdf since this is a read-only operation.
   * @param processedData
   * @returns pdf including all retrieved binders, or 404 if no binders are found.
   */
  async generatePDF(
    processedData: BinderLabelContext[]
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    return this.getPdf(processedData);
  }

  extractBase64PDF(apiResponse: ApiResponse<DocumentBundleResponse>): string {
    return apiResponse.payload.pdfResponse.base64Pdf;
  }

  extractPageRanges(
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): Array<{ start: number; end?: number }> | undefined {
    return apiResponse.payload.pdfResponse.pageRanges;
  }

  createOutline(
    rawData: JudicialBinderRawData,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem[] {
    this.count = 0;
    const binderFileIds = new Set(
      apiResponse.payload.binders.map((b) => b.labels.physicalFileId)
    );

    return rawData
      .filter((item) => binderFileIds.has(item.fileNumber))
      .map((item) => this.makeBinderGroup(item.fileNumber, apiResponse));
  }

  private makeBinderGroup(
    fileNumber: string,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem {
    const binders = apiResponse.payload.binders.filter(
      (b) => b.labels.physicalFileId === fileNumber
    );

    const children = binders.flatMap((binder) =>
      binder.documents
        .filter((doc) => doc.documentId) // Only include documents with valid IDs
        .map((doc) => this.makeDocElement(doc, apiResponse))
    );

    return {
      title: fileNumber,
      isExpanded: true,
      children,
    };
  }

  private makeDocElement(
    doc: BinderDocument,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem {
    return {
      title: doc.fileName ?? doc.documentType,
      pageIndex:
        apiResponse.payload.pdfResponse.pageRanges?.[this.count++]?.start,
    };
  }

  cleanup(): void {
    this.binderStore.clearBundles();
  }
}
