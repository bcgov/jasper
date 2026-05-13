import { OutlineItem, PDFViewerStrategy } from './PDFViewerTypes';
import { BinderService } from '@/services';
import { useCriminalDocumentBundleStore } from '@/stores';
import { CriminalDocumentAppearanceRequest } from '@/stores/CriminalDocumentBundleStore';
import { ApiResponse } from '@/types/ApiResponse';
import { BinderDocument } from '@/types/BinderDocument';
import { CriminalDocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { DocumentBundleResponse } from '@/types/DocumentBundleResponse';
import { inject } from 'vue';

export class CriminalDocumentPDFStrategy implements PDFViewerStrategy<
  Record<string, Record<string, CriminalDocumentAppearanceRequest[]>>,
  CriminalDocumentBundleRequest,
  ApiResponse<DocumentBundleResponse>
> {
  private readonly bundleStore = useCriminalDocumentBundleStore();
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

  getRawData(): Record<
    string,
    Record<string, CriminalDocumentAppearanceRequest[]>
  > {
    const appearanceRequests = this.bundleStore.getAppearanceRequests;
    const groupedRequests: Record<
      string,
      Record<string, CriminalDocumentAppearanceRequest[]>
    > = {};

    appearanceRequests.forEach((req) => {
      const fileNumber = req.fileNumber;
      const fullName = req.fullName ?? '';

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
    rawData: Record<string, Record<string, CriminalDocumentAppearanceRequest[]>>
  ): CriminalDocumentBundleRequest {
    const groupedAppearances = Object.values(rawData).flatMap((fileGroup) =>
      Object.values(fileGroup).flatMap((appearances) =>
        appearances.map((a) => a.appearance)
      )
    );

    return {
      appearances: groupedAppearances,
    } as unknown as CriminalDocumentBundleRequest;
  }

  /**
   * Retrieves or creates new binder(s) with all bundled documents.
   * For criminal key documents, we use generatePDF to create/view binders.
   * @param processedData
   * @returns pdf including retrieved or newly created binders
   */
  async generatePDF(
    processedData: CriminalDocumentBundleRequest
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    const urlParams = new URLSearchParams(globalThis.location.search);
    const documentCategories = urlParams.get('category')?.split(',') || [];
    return await this.binderService.generateBinderPDF(
      processedData,
      documentCategories
    );
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
    rawData: Record<
      string,
      Record<string, CriminalDocumentAppearanceRequest[]>
    >,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem[] {
    this.count = 0;
    const binderFileIds = new Set(
      apiResponse.payload.binders.map((b) => b.labels.physicalFileId)
    );

    return Object.entries(rawData)
      .filter(([, userGroup]) =>
        Object.values(userGroup)
          .flat()
          .some((req) => binderFileIds.has(req.appearance.physicalFileId))
      )
      .map(([groupKey, userGroup]) =>
        this.makeFirstGroup(groupKey, userGroup, apiResponse)
      );
  }

  private makeFirstGroup(
    groupKey: string,
    userGroup: Record<string, CriminalDocumentAppearanceRequest[]>,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem {
    const children: OutlineItem[] = [];

    Object.entries(userGroup).forEach(([name, docs]) => {
      const secondGroup = this.makeSecondGroup(name, docs, apiResponse);
      if (!secondGroup) {
        return;
      }
      if (name) {
        // Keep second grouping if name exists
        children.push(secondGroup);
      } else if (secondGroup.children) {
        // Flatten: add documents directly to first group
        children.push(...secondGroup.children);
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
    docs: CriminalDocumentAppearanceRequest[],
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem | null {
    const fileIds = docs.map((d) => d.appearance.physicalFileId);
    const partIds = docs.map((d) => d.appearance.participantId);

    const binders = apiResponse.payload.binders.filter(
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
        .filter((doc) => doc.documentId) // Only include documents with valid IDs
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
