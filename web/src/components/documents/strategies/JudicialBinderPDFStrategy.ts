import { BaseStoreBackedPDFStrategy } from './BaseStoreBackedPDFStrategy';
import { OutlineItem } from './PDFViewerTypes';
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
    rawData: JudicialBinderDocumentRequest[],
    _sessionId?: string
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
    apiResponse: ApiResponse<DocumentBundleResponse>,
    _sessionId?: string
  ): OutlineItem[] {
    const pageIndexByDocumentKey = this.buildPageIndexMap(apiResponse);

    const outlineEntries = rawData
      .map((item) => this.createOutlineEntry(item, pageIndexByDocumentKey))
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

    return buildGroupedEntriesOutline(outlineEntries);
  }

  override cleanup(sessionId?: string): void {
    this.store.clearBundles(sessionId);
  }

  private getDocumentCategoriesFromUrl(): string[] {
    const urlParams = new URLSearchParams(globalThis.location.search);

    return urlParams.get('category')?.split(',').filter(Boolean) ?? [];
  }

  private buildPageIndexMap(
    apiResponse: ApiResponse<DocumentBundleResponse>
  ): Map<string, number> {
    const pageIndexByDocumentKey = new Map<string, number>();
    let pageRangeIndex = 0;

    // Backend binder responses currently preserve the same document order used
    // to build pdfResponse.pageRanges, so positional correlation is intentional.
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

  private createOutlineEntry(
    item: JudicialBinderDocumentRequest,
    pageIndexByDocumentKey: Map<string, number>
  ):
    | {
        groupKeyOne: string;
        groupKeyTwo: string;
        title: string;
        pageIndex: number;
      }
    | undefined {
    const pageIndex = pageIndexByDocumentKey.get(
      this.makeDocumentKey(
        item.physicalFileId,
        item.participantId,
        item.documentId
      )
    );

    if (pageIndex === undefined) {
      return undefined;
    }

    return {
      groupKeyOne: item.groupKeyOne,
      groupKeyTwo: item.groupKeyTwo,
      title: item.documentName || 'Document',
      pageIndex,
    };
  }

  private makeDocumentKey(
    physicalFileId: string | undefined,
    participantId: string | undefined,
    documentId: string
  ): string {
    return [physicalFileId, participantId, documentId].join('|');
  }

  private compactLabels(
    labels: Record<string, string | undefined>
  ): Record<string, string> {
    return Object.fromEntries(
      Object.entries(labels).filter(
        ([, value]) => value !== undefined && value !== ''
      )
    );
  }
}
