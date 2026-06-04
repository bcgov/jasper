import type { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import {
  type EmbeddedOutlineAwarePDFViewerStrategy,
  type FileMetadataDto,
  type TransitoryMergeContext,
  type TransitoryViewerPayload,
} from '@/types/transitory-documents';
import type { OutlineItem, PDFViewerStrategy } from './PDFViewerTypes';

interface MergedPdfResponse {
  base64Pdf: string;
  pageRanges: Array<{ start: number; end?: number }>;
}

export class TransitoryBundleStrategy
  implements
    PDFViewerStrategy<FileMetadataDto[], FileMetadataDto[], MergedPdfResponse>,
    EmbeddedOutlineAwarePDFViewerStrategy<FileMetadataDto[], MergedPdfResponse>
{
  private readonly transitoryDocumentsService: TransitoryDocumentsService;
  private readonly storageKey: string;
  private documentsData: FileMetadataDto[] = [];
  private mergeContext?: TransitoryMergeContext;

  constructor(
    transitoryDocumentsService: TransitoryDocumentsService,
    storageKey: string
  ) {
    this.transitoryDocumentsService = transitoryDocumentsService;
    this.storageKey = `transitoryDocuments:${storageKey}`;

    // Load documents data from sessionStorage
    const storedData = sessionStorage.getItem(this.storageKey);
    if (storedData) {
      try {
        this.hydrateStoredPayload(JSON.parse(storedData));
      } catch (error) {
        console.error('Failed to parse transitory documents data:', error);
      }
    }
  }

  private hydrateStoredPayload(payload: unknown): void {
    if (Array.isArray(payload)) {
      // Backward compatibility: older payloads stored only files array.
      this.documentsData = payload as FileMetadataDto[];
      this.mergeContext = undefined;
      return;
    }

    const candidate = payload as Partial<TransitoryViewerPayload>;
    if (candidate && Array.isArray(candidate.files)) {
      this.documentsData = candidate.files;
      this.mergeContext = candidate.context;
      return;
    }

    this.documentsData = [];
    this.mergeContext = undefined;
  }

  hasData(): boolean {
    return this.documentsData.length > 0;
  }

  getRawData(): FileMetadataDto[] {
    return this.documentsData;
  }

  processDataForAPI(rawData: FileMetadataDto[]): FileMetadataDto[] {
    return rawData;
  }

  async generatePDF(
    processedData: FileMetadataDto[]
  ): Promise<MergedPdfResponse> {
    // Send file metadata directly to backend for merging
    // The backend will download and merge all PDFs efficiently in one operation
    const mergedResult = await this.transitoryDocumentsService.mergePdfs(
      processedData,
      this.mergeContext
    );

    return mergedResult;
  }

  extractBase64PDF(apiResponse: MergedPdfResponse): string {
    return apiResponse.base64Pdf;
  }

  extractPageRanges(
    apiResponse: MergedPdfResponse
  ): Array<{ start: number; end?: number }> | undefined {
    return apiResponse.pageRanges;
  }

  createOutline(
    rawData: FileMetadataDto[],
    apiResponse: MergedPdfResponse
  ): OutlineItem[] | undefined {
    return this.createOutlineWithEmbeddedOutline(rawData, apiResponse);
  }

  createOutlineWithEmbeddedOutline(
    rawData: FileMetadataDto[],
    apiResponse: MergedPdfResponse,
    embeddedOutline?: OutlineItem[]
  ): OutlineItem[] | undefined {
    return rawData.map((doc, index) => {
      const pageRange = apiResponse.pageRanges?.[index];
      const pageStart = pageRange?.start;
      const pageEnd =
        pageRange?.end ?? apiResponse.pageRanges?.[index + 1]?.start;

      return {
        title: doc.fileName,
        pageIndex: pageStart,
        isExpanded: true,
        children: this.filterEmbeddedOutlineForRange(
          embeddedOutline,
          pageStart,
          pageEnd
        ),
      };
    });
  }

  private filterEmbeddedOutlineForRange(
    embeddedOutline: OutlineItem[] | undefined,
    pageStart: number | undefined,
    pageEnd: number | undefined
  ): OutlineItem[] | undefined {
    if (!embeddedOutline?.length || pageStart === undefined) {
      return undefined;
    }

    const matchingItems = embeddedOutline.flatMap((item) =>
      this.filterOutlineItemForRange(item, pageStart, pageEnd)
    );

    return matchingItems.length > 0 ? matchingItems : undefined;
  }

  private filterOutlineItemForRange(
    item: OutlineItem,
    pageStart: number,
    pageEnd: number | undefined
  ): OutlineItem[] {
    const filteredChildren = item.children?.flatMap((child) =>
      this.filterOutlineItemForRange(child, pageStart, pageEnd)
    );
    const hasPageTarget = item.pageIndex !== undefined;
    const isInRange =
      hasPageTarget &&
      item.pageIndex >= pageStart &&
      (pageEnd === undefined || item.pageIndex < pageEnd);

    if (isInRange) {
      return [
        {
          ...item,
          isExpanded: true,
          children: filteredChildren?.length ? filteredChildren : undefined,
        },
      ];
    }

    if (!filteredChildren?.length) {
      return [];
    }

    if (hasPageTarget) {
      return filteredChildren;
    }

    return [
      {
        ...item,
        isExpanded: true,
        children: filteredChildren,
      },
    ];
  }

  cleanup(): void {
    // Clean up sessionStorage
    sessionStorage.removeItem(this.storageKey);
    this.documentsData = [];
  }
}
