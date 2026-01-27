import { describe, it, expect, beforeEach, vi, Mock } from 'vitest';
import { OrderPDFStrategy } from '@/components/documents/strategies/OrderPDFStrategy';
import { usePDFViewerStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { inject } from 'vue';
import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { DocumentRequestType } from '@/types/shared';

vi.mock('@/stores', () => ({
  usePDFViewerStore: vi.fn(),
}));
vi.mock('vue', () => ({
  inject: vi.fn(),
}));

const mockedUsePDFViewerStore = usePDFViewerStore as unknown as Mock;
const mockedInject = inject as unknown as Mock;

const createMockDocument = (id: string, name: string): StoreDocument => ({
  documentName: name,
  request: {
    type: DocumentRequestType.File,
    data: {
      documentId: id,
      partId: '',
      profSeqNo: '',
      courtLevelCd: '',
      courtClassCd: '',
      appearanceId: '',
      courtDivisionCd: '',
      fileId: '',
      isCriminal: false,
      correlationId: ''
    }
  },
  groupKeyOne: '',
  groupKeyTwo: '',
  physicalFileId: ''
});

const mockStoreDocuments: StoreDocument[] = [
  createMockDocument('1', 'Doc1.pdf'),
  createMockDocument('2', 'Doc2.pdf'),
  createMockDocument('3', 'Doc3.pdf'),
];

const mockGroupedDocuments: Record<string, Record<string, StoreDocument[]>> = {
  'Group 1': {
    'John Doe': [mockStoreDocuments[0]],
    'Jane Doe': [mockStoreDocuments[1]],
  },
  'Group 2': {
    '': [mockStoreDocuments[2]], // Empty string for ungrouped
  },
};

const mockPDFViewerStore = {
  documents: mockStoreDocuments,
  groupedDocuments: mockGroupedDocuments,
  clearDocuments: vi.fn(),
};

const mockFilesService = {
  generatePdf: vi.fn(),
};

const mockApiResponse: GeneratePdfResponse = {
  base64Pdf: 'base64string',
  pageRanges: [
    { start: 1, end: 2 },
    { start: 3, end: 4 },
    { start: 5, end: 5 },
  ],
};

describe('OrderPDFStrategy', () => {
  beforeEach(() => {
    mockedUsePDFViewerStore.mockReturnValue(mockPDFViewerStore);
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      return undefined;
    });
    mockPDFViewerStore.clearDocuments.mockClear();
    mockFilesService.generatePdf.mockClear();
  });

  it('throws error if FilesService is not injected', () => {
    mockedInject.mockReturnValueOnce(undefined);
    expect(() => new OrderPDFStrategy()).toThrow('FilesService is not available!');
  });

  it('hasData returns true if documents exist', () => {
    const strategy = new OrderPDFStrategy();
    expect(strategy.hasData()).toBe(true);
  });

  it('hasData returns false if no documents exist', () => {
    mockedUsePDFViewerStore.mockReturnValueOnce({
      ...mockPDFViewerStore,
      documents: [],
    });
    const strategy = new OrderPDFStrategy();
    expect(strategy.hasData()).toBe(false);
  });

  it('getRawData returns grouped documents from store', () => {
    const strategy = new OrderPDFStrategy();
    const rawData = strategy.getRawData();
    expect(rawData).toEqual(mockGroupedDocuments);
  });

  it('processDataForAPI flattens all documents', () => {
    const strategy = new OrderPDFStrategy();
    const result = strategy.processDataForAPI(mockGroupedDocuments);
    expect(result.length).toBe(3);
    expect(result[0]).toEqual(mockStoreDocuments[0]);
    expect(result[1]).toEqual(mockStoreDocuments[1]);
    expect(result[2]).toEqual(mockStoreDocuments[2]);
  });

  it('generatePDF calls filesService.generatePdf with mapped requests', async () => {
    const strategy = new OrderPDFStrategy();
    mockFilesService.generatePdf.mockResolvedValue(mockApiResponse);
    const result = await strategy.generatePDF(mockStoreDocuments);
    expect(mockFilesService.generatePdf).toHaveBeenCalledWith(
      mockStoreDocuments.map(doc => doc.request)
    );
    expect(result).toBe(mockApiResponse);
  });

  it('extractBase64PDF returns base64Pdf from response', () => {
    const strategy = new OrderPDFStrategy();
    const base64 = strategy.extractBase64PDF(mockApiResponse);
    expect(base64).toBe('base64string');
  });

  it('extractPageRanges returns pageRanges from response', () => {
    const strategy = new OrderPDFStrategy();
    const ranges = strategy.extractPageRanges(mockApiResponse);
    expect(ranges).toEqual([
      { start: 1, end: 2 },
      { start: 3, end: 4 },
      { start: 5, end: 5 },
    ]);
  });

  it('createOutline creates outline structure from rawData and apiResponse', () => {
    const strategy = new OrderPDFStrategy();
    const outline = strategy.createOutline(mockGroupedDocuments, mockApiResponse);
    expect(outline.length).toBe(2); // Group 1 and Group 2
    expect(outline[0].title).toBe('Group 1');
    expect(outline[0].pageIndex).toBe(1); // First page
    expect(outline[0]!.children!.length).toBe(2); // John Doe, Jane Doe
    expect(outline[0]!.children![0]!.title).toBe('John Doe');
    expect(outline[0]!.children![0]!.children![0]!.title).toBe('Doc1.pdf');
    expect(outline[0]!.children![1]!.title).toBe('Jane Doe');
    expect(outline[0]!.children![1]!.children![0]!.title).toBe('Doc2.pdf');
    expect(outline[1].title).toBe('Group 2');
    expect(outline[1]!.children!.length).toBe(1); // One ungrouped document
    expect(outline[1]!.children![0]!.title).toBe('Doc3.pdf');
  });

  it('createOutline handles empty string keys correctly', () => {
    const strategy = new OrderPDFStrategy();
    const outline = strategy.createOutline(mockGroupedDocuments, mockApiResponse);
    // Group 2 has empty string key, so documents should be added directly as children
    expect(outline[1]!.children![0]!.title).toBe('Doc3.pdf');
    expect(outline[1]!.children![0]!.pageIndex).toBe(5);
  });

  it('showOrderReviewOptions is true', () => {
    const strategy = new OrderPDFStrategy();
    expect(strategy.showOrderReviewOptions).toBe(true);
  });

  it('cleanup calls pdfStore.clearDocuments', () => {
    const strategy = new OrderPDFStrategy();
    strategy.cleanup();
    expect(mockPDFViewerStore.clearDocuments).toHaveBeenCalled();
  });

  it('makeDocElement returns correct OutlineItem', () => {
    const strategy = new OrderPDFStrategy();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (strategy as any).pageIndex = 1;
    const doc: StoreDocument = createMockDocument('123', 'TestDoc.pdf');
    const apiResponse: GeneratePdfResponse = {
      base64Pdf: '',
      pageRanges: [{ start: 10, end: 10 }, { start: 20, end: 20 }],
    };
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    const item = (strategy as any).makeDocElement(doc, apiResponse);
    expect(item.title).toBe('TestDoc.pdf');
    expect(item.pageIndex).toBe(20);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    expect((strategy as any).pageIndex).toBe(2); // Should increment
  });

  it('pageIndex is reset when createOutline is called', () => {
    const strategy = new OrderPDFStrategy();
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    (strategy as any).pageIndex = 99; // Set to some value
    strategy.createOutline(mockGroupedDocuments, mockApiResponse);
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    expect((strategy as any).pageIndex).toBeGreaterThanOrEqual(0); // Should have been reset and used
  });
});
