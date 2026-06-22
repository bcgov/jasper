import { BaseStoreBackedPDFStrategy } from './BaseStoreBackedPDFStrategy';
import { OutlineItem, PDFViewerInformationContext } from './PDFViewerTypes';
import { buildGroupedEntriesOutline } from './OutlineBuilder';
import { BinderService } from '@/services';
import { useJudicialBinderStore } from '@/stores';
import { JudicialBinderDocumentRequest } from '@/stores/JudicialBinderStore';
import { ApiResponse } from '@/types/ApiResponse';
import { DocumentBundleResponse } from '@/types/DocumentBundleResponse';
import { inject } from 'vue';

export type BinderLabelContext = Record<string, string>;

export class JudicialBinderPDFStrategy extends BaseStoreBackedPDFStrategy<
  JudicialBinderDocumentRequest,
  BinderLabelContext[],
  ApiResponse<DocumentBundleResponse>
> {
  protected readonly store = useJudicialBinderStore();
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
    rawData: JudicialBinderDocumentRequest[]
  ): BinderLabelContext[] {
    const labelContexts = new Map<string, BinderLabelContext>();

    for (const item of rawData) {
      const labels = this.compactLabels(item.labels);
      const entries = Object.entries(labels).sort(([a], [b]) =>
        a.localeCompare(b)
      );

      labelContexts.set(JSON.stringify(entries), labels);
    }

    return [...labelContexts.values()];
  }

  async generatePDF(
    processedData: BinderLabelContext[]
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    return await this.getPdf(processedData);
  }

  async getPdf(
    processedData: BinderLabelContext[]
  ): Promise<ApiResponse<DocumentBundleResponse>> {
    const documentCategories = this.getDocumentCategoriesFromUrl();

    return await this.binderService.viewBinderPDF(
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
    rawData: JudicialBinderDocumentRequest[],
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): OutlineItem[] {
    const outlineEntries = this.createOutlineEntriesInOutlineOrder(
      rawData,
      apiResponse
    );

    return buildGroupedEntriesOutline(outlineEntries);
  }

  resolveInformationContext(
    rawData: JudicialBinderDocumentRequest[]
  ): PDFViewerInformationContext | undefined {
    const item = rawData.find((request) => request.physicalFileId);

    if (!item) {
      return undefined;
    }

    return {
      physicalFileId: item.physicalFileId,
      isCriminal: item.labels.isCriminal?.toLowerCase() === 'true',
    };
  }

  override cleanup(sessionId?: string): void {
    this.store.clearBundles(sessionId);
  }

  private getDocumentCategoriesFromUrl(): string[] {
    const urlParams = new URLSearchParams(globalThis.location.search);

    return urlParams.get('category')?.split(',').filter(Boolean) ?? [];
  }

  private createOutlineEntriesInOutlineOrder(
    rawData: JudicialBinderDocumentRequest[],
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): Array<{
    groupKeyOne: string;
    groupKeyTwo: string;
    title: string;
    pageIndex: number;
  }> {
    const includedDocumentKeys = this.getIncludedDocumentKeys(apiResponse);

    return rawData
      .filter((item) =>
        includedDocumentKeys.has(
          this.makeDocumentKey(
            item.physicalFileId,
            item.participantId,
            item.documentId
          )
        )
      )
      .map((item, index) => {
        const pageIndex =
          apiResponse.payload.pdfResponse.pageRanges?.[index]?.start;

        if (pageIndex === undefined) {
          return undefined;
        }

        return {
          groupKeyOne: item.groupKeyOne,
          groupKeyTwo: item.groupKeyTwo,
          title: item.documentName || 'Document',
          pageIndex,
        };
      })
      .filter(
        (
          entry
        ): entry is {
          groupKeyOne: string;
          groupKeyTwo: string;
          title: string;
          pageIndex: number;
        } => entry !== undefined
      );
  }

  private getIncludedDocumentKeys(
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): Set<string> {
    const documentCategories = this.getDocumentCategoriesFromUrl();
    const includedDocumentKeys = new Set<string>();

    apiResponse.payload.binders.forEach((binder) => {
      binder.documents
        .filter((document) => document.documentId)
        .filter((document) =>
          this.matchesSelectedCategories(document.category, documentCategories)
        )
        .forEach((document) => {
          includedDocumentKeys.add(
            this.makeDocumentKey(
              binder.labels.physicalFileId,
              binder.labels.participantId,
              document.documentId
            )
          );
        });
    });

    return includedDocumentKeys;
  }

  private makeDocumentKey(
    physicalFileId: string | undefined,
    participantId: string | undefined,
    documentId: string
  ): string {
    return [physicalFileId ?? '', participantId ?? '', documentId].join('|');
  }

  private compactLabels(
    labels: Record<string, string | undefined>
  ): Record<string, string> {
    return Object.fromEntries(
      Object.entries(labels).filter(
        (entry): entry is [string, string] =>
          entry[1] !== undefined && entry[1] !== ''
      )
    );
  }

  private matchesSelectedCategories(
    category: string | undefined,
    documentCategories: string[]
  ): boolean {
    return (
      documentCategories.length === 0 ||
      documentCategories.some(
        (documentCategory) =>
          documentCategory.toLowerCase() === category?.toLowerCase()
      )
    );
  }
}
