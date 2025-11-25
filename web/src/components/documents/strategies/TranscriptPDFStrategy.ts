import {
  OutlineItem,
  PDFViewerStrategy,
} from '@/components/documents/FileViewer.vue';
import { DarsService } from '@/services/DarsService';
import { inject } from 'vue';

export interface TranscriptDocument {
  orderId: string;
  documentId: string;
  documentName: string;
}

export interface TranscriptPDFResponse {
  base64Pdf: string;
}

export class TranscriptPDFStrategy
  implements
    PDFViewerStrategy<
      TranscriptDocument[],
      TranscriptDocument,
      TranscriptPDFResponse
    >
{
  private readonly darsService: DarsService;

  constructor() {
    const service = inject<DarsService>('darsService');
    if (!service) {
      throw new Error('DarsService is not available!');
    }
    this.darsService = service;
  }

  hasData(): boolean {
    return false; // Transcripts are viewed individually, not bundled
  }

  getRawData(): TranscriptDocument[] {
    return [];
  }

  processDataForAPI(rawData: TranscriptDocument[]): TranscriptDocument {
    // For single document viewing, return the first document
    return rawData[0];
  }

  async generatePDF(
    processedData: TranscriptDocument
  ): Promise<TranscriptPDFResponse> {
    const base64Pdf = await this.darsService.getTranscriptDocument(
      processedData.orderId,
      processedData.documentId
    );
    return { base64Pdf };
  }

  extractBase64PDF(apiResponse: TranscriptPDFResponse): string {
    return apiResponse.base64Pdf;
  }

  extractPageRanges(
    apiResponse: TranscriptPDFResponse
  ): Array<{ start: number; end?: number }> | undefined {
    return undefined; // Single document, no page ranges needed
  }

  createOutline(
    rawData: TranscriptDocument[],
    apiResponse: TranscriptPDFResponse
  ): OutlineItem[] {
    if (rawData.length === 0) {
      return [];
    }

    return [
      {
        title: rawData[0].documentName,
        pageIndex: 1,
        isExpanded: true,
      },
    ];
  }

  cleanup(): void {
    // No store to clean up for individual transcript viewing
  }
}
