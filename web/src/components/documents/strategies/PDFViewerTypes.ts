export type OutlineItem = {
  title: string;
  pageIndex?: number;
  isExpanded?: boolean;
  children?: OutlineItem[];
};

export interface PDFViewerStrategy<TRawData, TProcessedData, TApiResponse> {
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

  reviewOrder?(orderReview: unknown): Promise<void>;

  cleanup(sessionId?: string): void;
}
