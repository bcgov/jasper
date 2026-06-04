import { BaseStoreBackedPDFStrategy } from './BaseStoreBackedPDFStrategy';
import { OutlineItem } from './PDFViewerTypes';
import {
  buildGroupedEntriesOutline,
  GroupedOutlineEntry,
} from './OutlineBuilder';
import { BinderService } from '@/services';
import { useCriminalDocumentBundleStore } from '@/stores';
import { CriminalDocumentAppearanceRequest } from '@/stores/CriminalDocumentBundleStore';
import { ApiResponse } from '@/types/ApiResponse';
import { CriminalDocumentBundleRequest } from '@/types/DocumentBundleRequest';
import { DocumentBundleResponse } from '@/types/DocumentBundleResponse';
import { inject } from 'vue';

export class CriminalDocumentPDFStrategy extends BaseStoreBackedPDFStrategy<
  CriminalDocumentAppearanceRequest,
  CriminalDocumentBundleRequest,
  ApiResponse<DocumentBundleResponse>
> {
  protected readonly store = useCriminalDocumentBundleStore();
  private readonly binderService: BinderService;

  constructor() {
    super();

    const service = inject<BinderService>('binderService');

    if (!service) {
      throw new Error('BinderService is not available!');
    }

    this.binderService = service;
  }

  processDataForAPI(
    rawData: CriminalDocumentAppearanceRequest[]
  ): CriminalDocumentBundleRequest {
    return {
      appearances: rawData.map((item) => item.appearance),
    };
  }

  /**
   * Retrieves or creates binder(s) with all bundled documents.
   * For criminal key documents, this intentionally uses generateBinderPDF.
   */
  async generatePDF(
    processedData: CriminalDocumentBundleRequest
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    const documentCategories = this.getDocumentCategoriesFromUrl();

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
    rawData: CriminalDocumentAppearanceRequest[],
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem[] {
    const pageIndexByDocumentKey = this.buildPageIndexMap(apiResponse);

    return buildGroupedEntriesOutline(
      rawData.flatMap((item) =>
        this.getMatchingOutlineEntries(
          item,
          pageIndexByDocumentKey,
          apiResponse
        )
      )
    );
  }

  private getMatchingOutlineEntries(
    item: CriminalDocumentAppearanceRequest,
    pageIndexByDocumentKey: Map<string, number>,
    apiResponse: ApiResponse<DocumentBundleResponse>
  ) {
    const matchingBinders = apiResponse.payload.binders.filter((binder) => {
      return (
        binder.labels?.physicalFileId === item.appearance.physicalFileId &&
        binder.labels?.participantId === item.appearance.participantId
      );
    });

    return matchingBinders.flatMap((binder) =>
      binder.documents
        .filter((document) => document.documentId)
        .map((document) => {
          const documentKey = this.makeDocumentKey(
            binder.labels.physicalFileId,
            binder.labels.participantId,
            document.documentId
          );

          const pageIndex = pageIndexByDocumentKey.get(documentKey);

          if (pageIndex === undefined) {
            return undefined;
          }

          return {
            groupKeyOne: item.groupKeyOne,
            groupKeyTwo: item.groupKeyTwo,
            title:
              document.fileName ?? String(document.documentType ?? 'Document'),
            pageIndex,
          };
        })
        .filter((entry): entry is GroupedOutlineEntry => entry !== undefined)
    );
  }

  private buildPageIndexMap(
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): Map<string, number> {
    const pageIndexByDocumentKey = new Map<string, number>();
    let pageRangeIndex = 0;

    apiResponse.payload.binders.forEach((binder) => {
      binder.documents
        .filter((document) => document.documentId)
        .forEach((document) => {
          const pageIndex =
            apiResponse.payload.pdfResponse.pageRanges?.[pageRangeIndex++]
              ?.start;

          if (pageIndex === undefined) {
            return;
          }

          const documentKey = this.makeDocumentKey(
            binder.labels.physicalFileId,
            binder.labels.participantId,
            document.documentId
          );

          pageIndexByDocumentKey.set(documentKey, pageIndex);
        });
    });

    return pageIndexByDocumentKey;
  }

  private makeDocumentKey(
    physicalFileId: string | undefined,
    participantId: string | undefined,
    documentId: string
  ): string {
    return [physicalFileId, participantId, documentId].join('|');
  }
  private getDocumentCategoriesFromUrl(): string[] {
    const urlParams = new URLSearchParams(globalThis.location.search);

    return urlParams.get('category')?.split(',').filter(Boolean) ?? [];
  }
}
