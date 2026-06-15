import { OrderReview } from '@/types';
import { ToolbarItem } from '@nutrient-sdk/viewer';

export interface PDFViewerStrategy<
  TRawData = any,
  TProcessedData = any,
  TAPIResponse = any,
> {
  hasData(): boolean;
  getRawData(): TRawData;
  processDataForAPI(rawData: TRawData): TProcessedData;
  generatePDF(processedData: TProcessedData): Promise<TAPIResponse>;
  extractBase64PDF(apiResponse: TAPIResponse): string;
  extractPageRanges(
    apiResponse: TAPIResponse
  ): Array<{ start: number; end?: number }> | undefined;
  createOutline(rawData: TRawData, apiResponse: TAPIResponse): OutlineItem[];
  cleanup(): void;
  showOrderReviewOptions?: boolean;
  reviewOrder?(orderReview: OrderReview): Promise<void>;
  setToolbarItems?(items: ToolbarItem[]): ToolbarItem[];
}

export interface OutlineItem {
  title: string;
  pageIndex?: number;
  children?: OutlineItem[];
  isExpanded?: boolean;
  action?: any;
}
