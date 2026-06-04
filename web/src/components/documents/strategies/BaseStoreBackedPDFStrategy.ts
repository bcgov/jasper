import { OutlineItem, PDFViewerStrategy } from './PDFViewerTypes';
import { PdfSourceStore } from './PdfSourceStore';

export abstract class BaseStoreBackedPDFStrategy<
  TItem,
  TProcessedData,
  TApiResponse,
> implements PDFViewerStrategy<TItem[], TProcessedData, TApiResponse> {
  protected abstract readonly store: PdfSourceStore<TItem>;

  hasData(sessionId?: string): boolean {
    return this.store.hasPdfData(sessionId);
  }

  getRawData(sessionId?: string): TItem[] {
    return this.store.getPdfItems(sessionId);
  }

  abstract processDataForAPI(
    rawData: TItem[],
    sessionId?: string
  ): TProcessedData;

  abstract generatePDF(processedData: TProcessedData): Promise<TApiResponse>;

  abstract extractBase64PDF(apiResponse: TApiResponse): string;

  abstract extractPageRanges(
    apiResponse: TApiResponse
  ): Array<{ start: number; end?: number }> | undefined;

  abstract createOutline(
    rawData: TItem[],
    apiResponse: TApiResponse,
    sessionId?: string
  ): OutlineItem[];

  cleanup(sessionId?: string): void {
    this.store.clearPdfItems(sessionId);
  }
}
