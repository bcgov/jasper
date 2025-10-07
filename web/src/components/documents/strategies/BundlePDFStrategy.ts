import {
  OutlineItem,
  PDFViewerStrategy,
} from '@/components/documents/FileViewer.vue';
import { BinderService } from '@/services';
import { useBundleStore } from '@/stores';
import { appearanceRequest } from '@/stores/BundleStore';
import { ApiResponse } from '@/types/ApiResponse';
import { BinderDocument } from '@/types/BinderDocument';
import { DocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { DocumentBundleResponse } from '@/types/DocumentBundleResponse';
import { inject } from 'vue';

export class BundlePDFStrategy
  implements
    PDFViewerStrategy<
      Record<string, Record<string, appearanceRequest[]>>,
      DocumentBundleRequest,
      ApiResponse<DocumentBundleResponse>
    >
{
  private readonly bundleStore = useBundleStore();
  private readonly binderService: BinderService;
  private count = 0;

  constructor() {
    const service = inject<BinderService>('binderService');
    if (!service) {
      throw new Error('BinderService is not available!');
    }
    this.binderService = service;
  }

  hasData(): boolean {
    return !!this.bundleStore.getAppearanceRequests;
  }

  getRawData(): Record<string, Record<string, appearanceRequest[]>> {
    const appearanceRequests = this.bundleStore.getAppearanceRequests;
    const groupedRequests: Record<
      string,
      Record<string, appearanceRequest[]>
    > = {};

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

  processDataForAPI(
    rawData: Record<string, Record<string, appearanceRequest[]>>
  ): DocumentBundleRequest {
    const groupedAppearances = Object.values(rawData).flatMap((fileGroup) =>
      Object.values(fileGroup).flatMap((appearances) =>
        appearances.map((a) => a.appearance)
      )
    );

    return {
      appearances: groupedAppearances,
    } as unknown as DocumentBundleRequest;
  }

  async generatePDF(
    processedData: DocumentBundleRequest
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    return await this.binderService.generateBinderPDF(processedData);
  }

  extractBase64PDF(apiResponse: ApiResponse<DocumentBundleResponse>): string {
    return apiResponse.payload.pdfResponse.base64Pdf;
  }

  extractPageRanges(
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): Array<{ start: number; end?: number }> | undefined {
    return apiResponse.payload.pdfResponse.pageRanges;
  }

  createOutline(
    rawData: Record<string, Record<string, appearanceRequest[]>>,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem[] {
    this.count = 0; // Reset counter
    return Object.entries(rawData).map(([groupKey, userGroup]) =>
      this.makeFirstGroup(groupKey, userGroup, apiResponse)
    );
  }

  private makeFirstGroup(
    groupKey: string,
    userGroup: Record<string, appearanceRequest[]>,
    apiResponse: ApiResponse<DocumentBundleResponse>
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
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem | null {
    const fileIds = docs.map((d) => d.appearance.physicalFileId);
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
    const pageIndex =
      apiResponse.payload.pdfResponse.pageRanges?.[this.count]?.start;
    const children = binders.flatMap((binder) =>
      binder.documents
        .filter((doc) => !isNaN(parseFloat(doc.documentId)))
        .map((doc) => this.makeDocElement(doc, apiResponse))
    );

    return {
      title: memberName,
      isExpanded: true,
      children,
      pageIndex,
    };
  }

  private makeDocElement(
    doc: BinderDocument,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem {
    return {
      title: doc.fileName ?? doc.documentType,
      pageIndex:
        apiResponse.payload.pdfResponse.pageRanges?.[this.count++]?.start,
    };
  }

  cleanup(): void {
    this.bundleStore.clearBundles();
  }
}
