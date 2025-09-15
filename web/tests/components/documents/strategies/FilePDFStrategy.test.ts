import { describe, it, expect, vi, beforeEach } from 'vitest';
import { FilePDFStrategy } from '@/components/documents/strategies/FilePDFStrategy';
import { FilesService } from '@/services/FilesService';
import type { StoreDocument } from '@/stores/PDFViewerStore';
import type { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { inject } from 'vue';

vi.mock('vue', () => ({
  inject: vi.fn(),
}));
vi.mock('@/services/FilesService');
vi.mock('@/stores', () => ({
  usePDFViewerStore: () => mockPdfStore,
}));

class MockFilesService {
  generatePdf = vi.fn();
}

const mockPdfStore = {
  documents: [] as StoreDocument[],
  groupedDocuments: {},
  clearDocuments: vi.fn(),
};


describe('FilePDFStrategy', () => {
  let filesService: MockFilesService;
  let strategy: FilePDFStrategy;

  beforeEach(() => {
    filesService = {} as MockFilesService;
    (FilesService as any).mockReturnValue(filesService);
    mockPdfStore.documents = [];
    mockPdfStore.groupedDocuments = {};
    mockPdfStore.clearDocuments.mockClear();
    
    (inject as unknown as vi.mock).mockReturnValue(filesService);

    strategy = new FilePDFStrategy();
  });

  it('hasData returns false when no documents', () => {
    mockPdfStore.documents = [];
    expect(strategy.hasData()).toBe(false);
  });

  it('hasData returns true when documents exist', () => {
    mockPdfStore.documents = [{} as StoreDocument];
    expect(strategy.hasData()).toBe(true);
  });

  it('getRawData returns groupedDocuments', () => {
    mockPdfStore.groupedDocuments = { a: { b: [{} as StoreDocument] } };
    expect(strategy.getRawData()).toEqual({ a: { b: [{} as StoreDocument] } });
  });

  it('processDataForAPI flattens groupedDocuments', () => {
    const doc1 = { id: 1 } as unknown as StoreDocument;
    const doc2 = { id: 2 } as unknown as StoreDocument;
    const rawData = { group1: { user1: [doc1], user2: [doc2] } };
    expect(strategy.processDataForAPI(rawData)).toEqual([doc1, doc2]);
  });

  it('extractBase64PDF returns base64Pdf', () => {
    const apiResponse = { base64Pdf: 'abc', pageRanges: [] };
    expect(strategy.extractBase64PDF(apiResponse)).toBe('abc');
  });

  it('extractPageRanges returns pageRanges', () => {
    const apiResponse = { base64Pdf: '', pageRanges: [{ start: 1, end: 2 }] };
    expect(strategy.extractPageRanges(apiResponse)).toEqual([{ start: 1, end: 2 }]);
  });

  it('createOutline builds outline structure', () => {
    const doc1 = { documentName: 'Doc1' } as StoreDocument;
    const doc2 = { documentName: 'Doc2' } as StoreDocument;
    const rawData = { GroupA: { UserA: [doc1], '': [doc2] } };
    const apiResponse: GeneratePdfResponse = {
      base64Pdf: '',
      pageRanges: [{
          start: 1,
          end: 0
      }, {
          start: 2,
          end: 0
      }],
    };
    const outline = strategy.createOutline(rawData, apiResponse);
    expect(outline).toHaveLength(1);
    expect(outline[0].title).toBe('GroupA');
    expect(outline[0].children[0].title).toBe('UserA');
    expect(outline[0].children[1].title).toBe('Doc2');
    expect(outline[0].children[1].pageIndex).toBe(2);
  });

  it('cleanup calls pdfStore.clearDocuments', () => {
    strategy.cleanup();
    expect(mockPdfStore.clearDocuments).toHaveBeenCalled();
  });
});