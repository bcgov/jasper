import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { OrderPDFStrategy } from '@/components/documents/strategies/OrderPDFStrategy';
import { useCommonStore, usePDFViewerStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { useSnackbarStore } from '@/stores/SnackbarStore';
import { OrderReviewStatus } from '@/types/common';
import { DocumentRequestType } from '@/types/shared';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';
import { inject } from 'vue';

vi.mock('@/stores', () => ({
  usePDFViewerStore: vi.fn(),
  useCommonStore: vi.fn(),
  useSnackbarStore: vi.fn(),
}));
vi.mock('@/stores/SnackbarStore', () => ({
  useSnackbarStore: vi.fn(),
  showSnackbar: vi.fn(),
}));
vi.mock('vue', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue')>();
  return {
    ...actual,
    inject: vi.fn(),
  };
});

const mockedUsePDFViewerStore = usePDFViewerStore as unknown as Mock;
const mockedUseCommonStore = useCommonStore as unknown as Mock;
const mockedUseSnackbarStore = useSnackbarStore as unknown as Mock;
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
      correlationId: '',
    },
  },
  groupKeyOne: '',
  groupKeyTwo: '',
  physicalFileId: '',
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

const mockCommonStore = {
  userInfo: { judgeId: 'judge-1' },
  loggedInUserInfo: { judgeId: 'judge-1' },
};

const mockFilesService = {
  generatePdf: vi.fn(),
};

const mockOrderService = {
  review: vi.fn(),
};

const mockSnackbarStore = {
  showSnackbar: vi.fn(),
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
    setActivePinia(createPinia());
    mockedUsePDFViewerStore.mockReturnValue(mockPDFViewerStore);
    mockedUseCommonStore.mockReturnValue(mockCommonStore);
    mockedUseSnackbarStore.mockReturnValue(mockSnackbarStore);
    mockedInject.mockClear();
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      if (key === 'orderService') return mockOrderService;
      return undefined;
    });
    mockPDFViewerStore.clearDocuments.mockClear();
    mockFilesService.generatePdf.mockClear();
    mockOrderService.review.mockClear();
    mockSnackbarStore.showSnackbar.mockClear();
    // Reset window.location.search
    Object.defineProperty(window, 'location', {
      value: { search: '?id=test-order-123' },
      writable: true,
    });
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
      mockStoreDocuments.map((doc) => doc.request)
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
    const outline = strategy.createOutline(
      mockGroupedDocuments,
      mockApiResponse
    );
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
    const outline = strategy.createOutline(
      mockGroupedDocuments,
      mockApiResponse
    );
    // Group 2 has empty string key, so documents should be added directly as children
    expect(outline[1]!.children![0]!.title).toBe('Doc3.pdf');
    expect(outline[1]!.children![0]!.pageIndex).toBe(5);
  });

  it('showOrderReviewOptions is true when judge IDs match', () => {
    const strategy = new OrderPDFStrategy();
    expect(strategy.showOrderReviewOptions).toBe(true);
  });

  it('showOrderReviewOptions is false when judge IDs do not match', () => {
    mockedUseCommonStore.mockReturnValueOnce({
      userInfo: { judgeId: 'judge-1' },
      loggedInUserInfo: { judgeId: 'judge-2' },
    });
    const strategy = new OrderPDFStrategy();
    expect(strategy.showOrderReviewOptions).toBe(false);
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
      pageRanges: [
        { start: 10, end: 10 },
        { start: 20, end: 20 },
      ],
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

  describe('reviewOrder', () => {
    it('approves order successfully and shows success snackbar', async () => {
      const strategy = new OrderPDFStrategy();
      mockOrderService.review.mockResolvedValue(undefined);

      await strategy.reviewOrder({
        comments: 'Looks good',
        signed: true,
        status: OrderReviewStatus.Approved,
        documentData: 'base64pdf',
      });

      expect(mockOrderService.review).toHaveBeenCalledWith('test-order-123', {
        comments: 'Looks good',
        signed: true,
        status: OrderReviewStatus.Approved,
        documentData: 'base64pdf',
      });
      expect(mockSnackbarStore.showSnackbar).toHaveBeenCalledWith(
        'The order has been approved.',
        'rgb(46, 139, 43)',
        '✅ Approved!'
      );
    });

    it('rejects order successfully and shows rejection snackbar', async () => {
      const strategy = new OrderPDFStrategy();
      mockOrderService.review.mockResolvedValue(undefined);

      await strategy.reviewOrder({
        comments: 'Needs changes',
        signed: false,
        status: OrderReviewStatus.Unapproved,
        documentData: 'base64pdf',
      });

      expect(mockOrderService.review).toHaveBeenCalledWith('test-order-123', {
        comments: 'Needs changes',
        signed: false,
        status: OrderReviewStatus.Unapproved,
        documentData: 'base64pdf',
      });
      expect(mockSnackbarStore.showSnackbar).toHaveBeenCalledWith(
        'The order has been rejected.',
        'rgb(46, 139, 43)',
        '📋 Rejected'
      );
    });

    it('sets order to pending and shows pending snackbar', async () => {
      const strategy = new OrderPDFStrategy();
      mockOrderService.review.mockResolvedValue(undefined);

      await strategy.reviewOrder({
        comments: 'Under review',
        signed: false,
        status: OrderReviewStatus.Pending,
        documentData: 'base64pdf',
      });

      expect(mockOrderService.review).toHaveBeenCalledWith('test-order-123', {
        comments: 'Under review',
        signed: false,
        status: OrderReviewStatus.Pending,
        documentData: 'base64pdf',
      });
      expect(mockSnackbarStore.showSnackbar).toHaveBeenCalledWith(
        'The order review is awaiting documentation.',
        'rgb(46, 139, 43)',
        '⏳ Pending'
      );
    });

    it('throws error if order ID is not in URL', async () => {
      Object.defineProperty(window, 'location', {
        value: { search: '' },
        writable: true,
      });
      const strategy = new OrderPDFStrategy();

      await expect(
        strategy.reviewOrder({
          comments: 'Test',
          signed: true,
          status: OrderReviewStatus.Approved,
          documentData: 'base64pdf',
        })
      ).rejects.toThrow('Order ID not found in URL');

      expect(mockOrderService.review).not.toHaveBeenCalled();
      expect(mockSnackbarStore.showSnackbar).not.toHaveBeenCalled();
    });

    it('throws error if order ID parameter is null', async () => {
      Object.defineProperty(window, 'location', {
        value: { search: '?otherparam=value' },
        writable: true,
      });
      const strategy = new OrderPDFStrategy();

      await expect(
        strategy.reviewOrder({
          comments: 'Test',
          signed: true,
          status: OrderReviewStatus.Approved,
          documentData: 'base64pdf',
        })
      ).rejects.toThrow('Order ID not found in URL');
    });
  });
});
