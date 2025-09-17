import { inject } from 'vue';
import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { FilesService } from '@/services/FilesService';
import { usePDFViewerStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { PDFViewerStrategy, OutlineItem } from '@/components/documents/FileViewer.vue';

export class FilePDFStrategy implements PDFViewerStrategy<
  Record<string, Record<string, StoreDocument[]>>,
  StoreDocument[],
  GeneratePdfResponse 
> {
  private readonly pdfStore = usePDFViewerStore();
  private readonly filesService: FilesService;
  private pageIndex = 0;

  constructor() {
    const service = inject<FilesService>('filesService');
    if (!service) {
      throw new Error('FilesService is not available!');
    }
    this.filesService = service;
  }

  hasData(): boolean {
    return this.pdfStore.documents.length > 0;
  }

  getRawData(): Record<string, Record<string, StoreDocument[]>> {
    return this.pdfStore.groupedDocuments;
  }

  processDataForAPI(rawData: Record<string, Record<string, StoreDocument[]>>): StoreDocument[] {
    const allDocs: StoreDocument[] = [];
    Object.values(rawData).forEach((userGroup: Record<string, StoreDocument[]>) => {
      Object.values(userGroup).forEach((docs) => {
        allDocs.push(...(docs));
      });
    });
    return allDocs;
  }

  async generatePDF(processedData: StoreDocument[]): Promise<GeneratePdfResponse> {
    return await this.filesService.generatePdf(
      processedData.map((doc) => doc.request)
    );
  }

  extractBase64PDF(apiResponse: GeneratePdfResponse): string {
    return apiResponse.base64Pdf;
  }

  extractPageRanges(apiResponse: GeneratePdfResponse): Array<{ start: number; end?: number }> | undefined {
    return apiResponse.pageRanges;
  }

  createOutline(
    rawData: Record<string, Record<string, StoreDocument[]>>, 
    apiResponse: GeneratePdfResponse
  ): OutlineItem[] {
    this.pageIndex = 0; // Reset counter
    return Object.entries(rawData).map(([groupKey, userGroup]) =>
      this.makeFirstGroup(groupKey, userGroup, apiResponse)
    );
  }

  private makeFirstGroup(
    groupKey: string,
    userGroup: Record<string, StoreDocument[]>,
    apiResponse: GeneratePdfResponse
  ): OutlineItem {
    const children: OutlineItem[] = [];
    
    Object.entries(userGroup).forEach(([name, docs]) => {
      if (name !== '') {
        children.push(this.makeSecondGroup(name, docs, apiResponse));
      } else {
        children.push(...this.makeDocElements(docs, apiResponse));
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
    docs: StoreDocument[], 
    apiResponse: GeneratePdfResponse
  ): OutlineItem {
    return {
      title: memberName,
      isExpanded: true,
      children: docs.map((doc) => this.makeDocElement(doc, apiResponse)),
    };
  }

  private makeDocElements(docs: StoreDocument[], apiResponse: GeneratePdfResponse): OutlineItem[] {
    return docs.map((doc) => this.makeDocElement(doc, apiResponse));
  }

  private makeDocElement(doc: StoreDocument, apiResponse: GeneratePdfResponse): OutlineItem {
    return {
      title: doc.documentName,
      pageIndex: apiResponse.pageRanges?.[this.pageIndex++]?.start,
    };
  }

  cleanup(): void {
    this.pdfStore.clearDocuments();
  }
}