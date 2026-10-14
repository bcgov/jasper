import { OrderReview } from '@/types';
import type NutrientViewer from '@nutrient-sdk/viewer';
import { Instance, ToolbarItem } from '@nutrient-sdk/viewer';

export interface EmbeddedOutlineAwarePDFViewerStrategy<TRawData, TApiResponse> {
  createOutlineWithEmbeddedOutline(
    rawData: TRawData,
    apiResponse: TApiResponse,
    embeddedOutline?: OutlineItem[]
  ): OutlineItem[] | undefined;
}

export interface PDFViewerStrategy<
  TRawData = unknown,
  TProcessedData = unknown,
  TApiResponse = unknown,
> {
  hasData(sessionId?: string): boolean;

  getRawData(sessionId?: string): TRawData;

  processDataForAPI(rawData: TRawData): TProcessedData;

  generatePDF(
    processedData: TProcessedData,
    signal?: AbortSignal
  ): Promise<TApiResponse>;

  getLoadErrorMessage?(error: unknown): string | undefined;

  extractBase64PDF(apiResponse: TApiResponse): string;

  initialize?(): Promise<void>;

  extractPageRanges(
    apiResponse: TApiResponse
  ): Array<{ start: number; end?: number }> | undefined;

  createOutline(
    rawData: TRawData,
    apiResponse: TApiResponse
  ): OutlineItem[] | undefined;

  resolveInformationContext?(
    rawData: TRawData
  ): PDFViewerInformationContext | undefined;

  reviewOrder?(orderReview: OrderReview): Promise<void>;

  // Annotation descriptions that must be present to approve; undefined means any image annotation qualifies.
  getRequiredApprovalAnnotations?(): string[] | undefined;

  // Index of the API-appended page that is dropped on submit if the user left it untouched; undefined when none.
  getAppendedPageIndex?(totalPageCount: number): number | undefined;

  setToolbarItems?(
    items: ToolbarItem[],
    context: PDFViewerToolbarContext
  ): ToolbarItem[];

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

export interface PDFViewerToolbarContext {
  instance: Instance;
  nutrientViewer: typeof NutrientViewer;
  rawData: unknown;
  resolveInformationContext: (
    rawData: unknown
  ) => PDFViewerInformationContext | undefined;
  openReviewModal: () => void;
  updateCanApprove: () => Promise<void>;
}
