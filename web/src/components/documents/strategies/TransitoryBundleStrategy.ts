import {
  OutlineItem,
  PDFViewerStrategy,
} from '@/components/documents/FileViewer.vue';
import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import {
  FileMetadataDto,
  TransitoryMergeContext,
  TransitoryViewerPayload,
} from '@/types/transitory-documents';

interface MergedPdfResponse {
  base64Pdf: string;
  pageRanges: Array<{ start: number; end?: number }>;
}

export class TransitoryBundleStrategy implements PDFViewerStrategy<
  FileMetadataDto[],
  FileMetadataDto[],
  MergedPdfResponse
> {
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
  ): OutlineItem[] {
    return rawData.map((doc, index) => ({
      title: doc.fileName,
      pageIndex: apiResponse.pageRanges?.[index]?.start || index * 10,
      isExpanded: false,
    }));
  }

  cleanup(): void {
    // Clean up sessionStorage
    sessionStorage.removeItem(this.storageKey);
    this.documentsData = [];
  }
}
