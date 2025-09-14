import { inject } from 'vue';
import { CourtListService } from '@/services';
import { useBundleStore } from '@/stores';
import { appearanceRequest } from '@/stores/BundleStore';
import { ApiResponse } from '@/types/ApiResponse';
import { BinderDocument } from '@/types/BinderDocument';
import {
  CourtListDocumentBundleRequest,
  CourtListDocumentBundleResponse,
} from '@/types/courtlist/jsonTypes';
import { PDFViewerStrategy, OutlineItem } from '@/components/documents/shared/GenericPDFViewer.vue';

export class BundlePDFStrategy implements PDFViewerStrategy<
  Record<string, Record<string, appearanceRequest[]>>, // TRawData
  CourtListDocumentBundleRequest, // TProcessedData
  ApiResponse<CourtListDocumentBundleResponse> // TAPIResponse
> {
  private bundleStore = useBundleStore();
  private courtListService: CourtListService;
  private count = 0;

  constructor() {
    // Note: In a real implementation, you might want to inject this differently
    const service = inject<CourtListService>('courtListService');
    if (!service) {
      throw new Error('CourtListService is not available!');
    }
    this.courtListService = service;
  }

  hasData(): boolean {
    return !!this.bundleStore.getAppearanceRequests;
  }

  getRawData(): Record<string, Record<string, appearanceRequest[]>> {
    const appearanceRequests = this.bundleStore.getAppearanceRequests;
    const groupedRequests: Record<string, Record<string, appearanceRequest[]>> = {};

    appearanceRequests.forEach((req) => {
      const fileNumber = req.fileNumber;
      const fullName = req.fullName;

      if (!groupedRequests[fileNumber]) {
        groupedRequests[fileNumber] = {};
      }
      if (!groupedRequests[fileNumber][fullName]) {
        groupedRequests[fileNumber][fullName] = [];
      }
      groupedRequests[fileNumber][fullName].push(req);
    });

    return groupedRequests;
  }

  processDataForAPI(rawData: Record<string, Record<string, appearanceRequest[]>>): CourtListDocumentBundleRequest {
    const groupedAppearances = Object.values(rawData).flatMap(
      (fileGroup) =>
        Object.values(fileGroup).flatMap((appearances) =>
          appearances.map((a) => a.appearance)
        )
    );

    return {
      appearances: groupedAppearances,
    } as unknown as CourtListDocumentBundleRequest;
  }

  async generatePDF(processedData: CourtListDocumentBundleRequest): Promise<ApiResponse<CourtListDocumentBundleResponse>> {
    return await this.courtListService.generateCourtListPdf(processedData);
  }

  extractBase64PDF(apiResponse: ApiResponse<CourtListDocumentBundleResponse>): string {
    return apiResponse.payload.pdfResponse.base64Pdf;
  }

  extractPageRanges(apiResponse: ApiResponse<CourtListDocumentBundleResponse>): Array<{ start: number; end?: number }> | undefined {
    return apiResponse.payload.pdfResponse.pageRanges;
  }

  createOutline(
    rawData: Record<string, Record<string, appearanceRequest[]>>, 
    apiResponse: ApiResponse<CourtListDocumentBundleResponse>
  ): OutlineItem[] {
    this.count = 0; // Reset counter
    return Object.entries(rawData).map(([groupKey, userGroup]) =>
      this.makeFirstGroup(groupKey, userGroup, apiResponse)
    );
  }

  private makeFirstGroup(
    groupKey: string,
    userGroup: Record<string, appearanceRequest[]>,
    apiResponse: ApiResponse<CourtListDocumentBundleResponse>
  ): OutlineItem {
    const children: OutlineItem[] = [];
    
    Object.entries(userGroup).forEach(([name, docs]) => {
      const secondGroup = this.makeSecondGroup(name, docs, apiResponse);
      if (secondGroup) {
        children.push(secondGroup);
      }
    });

    return {
      title: groupKey,
      isExpanded: true,
      children,
    };
  }

  private makeSecondGroup(
    memberName: string, 
    docs: appearanceRequest[], 
    apiResponse: ApiResponse<CourtListDocumentBundleResponse>
  ): OutlineItem | null {
    const fileIds = docs.map((d) => d.appearance.fileId);
    const partIds = docs.map((d) => d.appearance.participantId);
    
    let binders = apiResponse.payload.binders.filter(
      (b) =>
        b.labels &&
        fileIds.some((id) => b.labels.physicalFileId === id) &&
        partIds.some((pid) => b.labels.participantId === pid)
    );
    
    if (!binders) {
      return null;
    }

    const children = binders.flatMap((binder) =>
      binder.documents
        .filter((doc) => !isNaN(parseFloat(doc.documentId)))
        .map((doc) => this.makeDocElement(doc, apiResponse))
    );

    return {
      title: memberName,
      isExpanded: true,
      children,
    };
  }

  private makeDocElement(
    doc: BinderDocument, 
    apiResponse: ApiResponse<CourtListDocumentBundleResponse>
  ): OutlineItem {
    console.log(doc);
    return {
      title: doc.fileName ?? doc.documentType,
      pageIndex: apiResponse.payload.pdfResponse.pageRanges?.[this.count++]?.start,
    };
  }
}