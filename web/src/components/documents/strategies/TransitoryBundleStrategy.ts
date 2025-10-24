import {
  OutlineItem,
  PDFViewerStrategy,
} from '@/components/documents/FileViewer.vue';
import { TransitoryDocumentsService } from '@/services/TransitoryDocumentsService';
import { FileMetadataDto } from '@/types/transitory-documents';
import { inject } from 'vue';

interface MergedPdfResponse {
  base64Pdf: string;
  pageRanges: Array<{ start: number; end?: number }>;
}

export class TransitoryBundleStrategy
  implements
    PDFViewerStrategy<FileMetadataDto[], FileMetadataDto[], MergedPdfResponse>
{
  private readonly transitoryDocumentsService: TransitoryDocumentsService;
  private documentsData: FileMetadataDto[] = [];

  constructor() {
    const service = inject<TransitoryDocumentsService>(
      'transitoryDocumentsService'
    );
    if (!service) {
      throw new Error('TransitoryDocumentsService is not available!');
    }
    this.transitoryDocumentsService = service;

    // Load documents data from sessionStorage
    const storedData = sessionStorage.getItem('transitoryDocuments');
    if (storedData) {
      try {
        this.documentsData = JSON.parse(storedData);
      } catch (error) {
        console.error('Failed to parse transitory documents data:', error);
      }
    }
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
    const mergedResult =
      await this.transitoryDocumentsService.mergePdfs(processedData);

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
    sessionStorage.removeItem('transitoryDocuments');
    this.documentsData = [];
  }
}
