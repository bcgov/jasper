import { OrderReview } from '@/types';
import { ToolbarItem } from '@nutrient-sdk/viewer';

export interface PDFViewerStrategy<
  TRawData = unknown,
  TProcessedData = unknown,
  TApiResponse = unknown,
> {
  showOrderReviewOptions?: boolean;

  hasData(sessionId?: string): boolean;

  getRawData(sessionId?: string): TRawData;

  processDataForAPI(rawData: TRawData): TProcessedData;

  generatePDF(processedData: TProcessedData): Promise<TApiResponse>;

  extractBase64PDF(apiResponse: TApiResponse): string;

  extractPageRanges(
    apiResponse: TApiResponse
  ): Array<{ start: number; end?: number }> | undefined;

  createOutline(rawData: TRawData, apiResponse: TApiResponse): OutlineItem[];

  resolveInformationContext?(
    rawData: TRawData
  ): PDFViewerInformationContext | undefined;

  reviewOrder?(orderReview: OrderReview): Promise<void>;

  setToolbarItems?(items: ToolbarItem[]): ToolbarItem[];

  cleanup(sessionId?: string): void;
}

export type OutlineItem = {
  title: string;
  pageIndex?: number;
  isExpanded?: boolean;
  children?: OutlineItem[];
};

export type PDFViewerInformationContext = {
  physicalFileId: string;
  isCriminal: boolean;
};
