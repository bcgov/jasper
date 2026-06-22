import { BaseStoreBackedPDFStrategy } from './BaseStoreBackedPDFStrategy';
import { OutlineItem } from './PDFViewerTypes';
import { buildGroupedEntriesOutline } from './OutlineBuilder';
import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { FilesService } from '@/services/FilesService';
import { usePDFViewerStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { inject } from 'vue';

export class FilePDFStrategy extends BaseStoreBackedPDFStrategy<
  StoreDocument,
  StoreDocument[],
  GeneratePdfResponse
> {
  protected readonly store = usePDFViewerStore();
  protected readonly filesService: FilesService;

  constructor() {
    super();

    const service = inject<FilesService>('filesService');

    if (!service) {
      throw new Error('FilesService is not available!');
    }

    this.filesService = service;
  }

  processDataForAPI(
    rawData: StoreDocument[],
    _sessionId?: string
  ): StoreDocument[] {
    return rawData;
  }

  async generatePDF(
    processedData: StoreDocument[]
  ): Promise<GeneratePdfResponse> {
    return await this.filesService.generatePdf(
      processedData.map((document) => document.request)
    );
  }

  extractBase64PDF(apiResponse: GeneratePdfResponse): string {
    return apiResponse.base64Pdf;
  }

  extractPageRanges(
    apiResponse: GeneratePdfResponse
  ): Array<{ start: number; end?: number }> | undefined {
    return apiResponse.pageRanges;
  }

  createOutline(
    rawData: StoreDocument[],
    apiResponse: GeneratePdfResponse,
    _sessionId?: string
  ): OutlineItem[] {
    return buildGroupedEntriesOutline(
      rawData.map((document, index) => ({
        groupKeyOne: document.groupKeyOne,
        groupKeyTwo: document.groupKeyTwo,
        title: this.getOutlineDocumentTitle(document),
        pageIndex: apiResponse.pageRanges?.[index]?.start,
      }))
    );
  }

  protected getOutlineDocumentTitle(document: StoreDocument): string {
    return document.documentName || 'Document';
  }
}
