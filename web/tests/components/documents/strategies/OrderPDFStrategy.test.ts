import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { OrderPDFStrategy } from '@/components/documents/strategies/OrderPDFStrategy';
import { useCommonStore, usePDFViewerStore, useSnackbarStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { OrderReview } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { DocumentRequestType } from '@/types/shared';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';
import { inject } from 'vue';

vi.mock('@/stores', () => ({
  usePDFViewerStore: vi.fn(),
  useSnackbarStore: vi.fn(),
  useCommonStore: vi.fn(),
}));
vi.mock('vue', async (importOriginal) => {
  const actual = await importOriginal<typeof import('vue')>();
  return {
    ...actual,
    inject: vi.fn(),
  };
});

const mockedUsePDFViewerStore = usePDFViewerStore as unknown as Mock;
const mockedUseSnackbarStore = useSnackbarStore as unknown as Mock;
const mockedUseCommonStore = useCommonStore as unknown as Mock;
const mockedInject = inject as unknown as Mock;

const createMockDocument = (
  id: string,
  name: string,
  groupKeyOne: string,
  groupKeyTwo = ''
): StoreDocument => ({
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
  groupKeyOne,
  groupKeyTwo,
  physicalFileId: '',
});

const mockStoreDocuments: StoreDocument[] = [
  createMockDocument('1', 'Doc1.pdf', 'Group 1', 'John Doe'),
  createMockDocument('2', 'Doc2.pdf', 'Group 1', 'Jane Doe'),
  createMockDocument('3', 'Doc3.pdf', 'Group 2'),
];

const mockPDFViewerStore = {
  hasPdfData: vi.fn(() => true),
  getPdfItems: vi.fn(() => mockStoreDocuments),
  clearPdfItems: vi.fn(),
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

const mockCommonStore = {
  userInfo: { judgeId: 11 },
  loggedInUserInfo: { judgeId: 11 },
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
    mockedUseSnackbarStore.mockReturnValue(mockSnackbarStore);
    mockedUseCommonStore.mockReturnValue(mockCommonStore);
    mockedInject.mockClear();
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      if (key === 'orderService') return mockOrderService;
      return undefined;
    });

    Object.defineProperty(globalThis, 'location', {
      value: { search: '?id=123' },
      writable: true,
      configurable: true,
    });

    mockPDFViewerStore.hasPdfData.mockImplementation(() => true);
    mockPDFViewerStore.getPdfItems.mockImplementation(() => mockStoreDocuments);
    mockPDFViewerStore.clearPdfItems.mockClear();
    mockFilesService.generatePdf.mockClear();
    mockOrderService.review.mockClear();
    mockSnackbarStore.showSnackbar.mockClear();
  });

  it('throws if OrderService is not injected', () => {
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      return undefined;
    });

    expect(() => new OrderPDFStrategy()).toThrow('Service(s) is undefined.');
  });

  it('hasData returns true if documents exist', () => {
    const strategy = new OrderPDFStrategy();
    expect(strategy.hasData()).toBe(true);
  });

  it('hasData returns false if no documents exist', () => {
    mockedUsePDFViewerStore.mockReturnValueOnce({
      ...mockPDFViewerStore,
      hasPdfData: vi.fn(() => false),
      getPdfItems: vi.fn(() => []),
      clearPdfItems: vi.fn(),
    });

    const strategy = new OrderPDFStrategy();
    expect(strategy.hasData()).toBe(false);
  });

  it('getRawData returns documents from store', () => {
    const strategy = new OrderPDFStrategy();
    expect(strategy.getRawData()).toEqual(mockStoreDocuments);
  });

  it('processDataForAPI returns raw documents unchanged', () => {
    const strategy = new OrderPDFStrategy();
    expect(strategy.processDataForAPI(mockStoreDocuments)).toEqual(
      mockStoreDocuments
    );
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
    expect(strategy.extractBase64PDF(mockApiResponse)).toBe('base64string');
  });

  it('extractPageRanges returns pageRanges from response', () => {
    const strategy = new OrderPDFStrategy();
    expect(strategy.extractPageRanges(mockApiResponse)).toEqual([
      { start: 1, end: 2 },
      { start: 3, end: 4 },
      { start: 5, end: 5 },
    ]);
  });

  it('createOutline groups documents by outline keys', () => {
    const strategy = new OrderPDFStrategy();
    const outline = strategy.createOutline(mockStoreDocuments, mockApiResponse);

    expect(outline).toHaveLength(2);
    expect(outline[0]).toMatchObject({ title: 'Group 1' });
    expect(outline[0]?.children).toHaveLength(2);
    expect(outline[0]?.children?.[0]).toMatchObject({ title: 'John Doe' });
    expect(outline[0]?.children?.[0]?.children?.[0]).toMatchObject({
      title: 'Doc1.pdf',
      pageIndex: 1,
    });
    expect(outline[0]?.children?.[1]?.children?.[0]).toMatchObject({
      title: 'Doc2.pdf',
      pageIndex: 3,
    });
    expect(outline[1]).toMatchObject({ title: 'Group 2' });
    expect(outline[1]?.children?.[0]).toMatchObject({
      title: 'Doc3.pdf',
      pageIndex: 5,
    });
  });

  it('createOutline starts page indexing from the first range on each call', () => {
    const strategy = new OrderPDFStrategy();
    const firstOutline = strategy.createOutline(
      mockStoreDocuments,
      mockApiResponse
    );
    const secondOutline = strategy.createOutline(
      mockStoreDocuments,
      mockApiResponse
    );

    expect(firstOutline[0]?.pageIndex).toBeUndefined();
    expect(firstOutline[0]?.children?.[0]?.children?.[0]?.pageIndex).toBe(1);
    expect(secondOutline[0]?.children?.[0]?.children?.[0]?.pageIndex).toBe(1);
  });

  it('cleanup calls the session-backed clearPdfItems store method', () => {
    const strategy = new OrderPDFStrategy();
    strategy.cleanup();
    expect(mockPDFViewerStore.clearPdfItems).toHaveBeenCalled();
  });

  it('showOrderReviewOptions is true when judge IDs match', () => {
    const strategy = new OrderPDFStrategy();

    expect(strategy.showOrderReviewOptions).toBe(true);
  });

  it('showOrderReviewOptions is false when judge IDs do not match', () => {
    mockedUseCommonStore.mockReturnValueOnce({
      userInfo: { judgeId: 11 },
      loggedInUserInfo: { judgeId: 12 },
    });

    const strategy = new OrderPDFStrategy();

    expect(strategy.showOrderReviewOptions).toBe(false);
  });

  it('approves order successfully and shows success snackbar', async () => {
    const strategy = new OrderPDFStrategy();
    const review: OrderReview = {
      comments: 'approved',
      signed: true,
      status: OrderReviewStatus.Approved,
      documentData: 'pdf-data',
      supportingDocumentData: '',
    };

    await strategy.reviewOrder(review);

    expect(mockOrderService.review).toHaveBeenCalledWith('123', review);
    expect(mockSnackbarStore.showSnackbar).toHaveBeenCalledWith(
      'The order has been approved.',
      'rgb(46, 139, 43)',
      'Approved!'
    );
  });

  it('rejects order successfully and shows rejection snackbar', async () => {
    const strategy = new OrderPDFStrategy();
    const review: OrderReview = {
      comments: 'rejected',
      signed: false,
      status: OrderReviewStatus.Unapproved,
      documentData: '',
      supportingDocumentData: '',
    };

    await strategy.reviewOrder(review);

    expect(mockOrderService.review).toHaveBeenCalledWith('123', review);
    expect(mockSnackbarStore.showSnackbar).toHaveBeenCalledWith(
      'The order has been rejected.',
      'rgb(46, 139, 43)',
      'Rejected'
    );
  });

  it('sets order to pending and shows pending snackbar', async () => {
    const strategy = new OrderPDFStrategy();
    const review: OrderReview = {
      comments: 'pending',
      signed: false,
      status: OrderReviewStatus.AwaitingDocumentation,
      documentData: '',
      supportingDocumentData: 'supporting-doc',
    };

    await strategy.reviewOrder(review);

    expect(mockOrderService.review).toHaveBeenCalledWith('123', review);
    expect(mockSnackbarStore.showSnackbar).toHaveBeenCalledWith(
      'The order review is awaiting documentation.',
      'rgb(46, 139, 43)',
      'Pending'
    );
  });

  it('throws error if order ID is not in URL', async () => {
    Object.defineProperty(globalThis, 'location', {
      value: { search: '' },
      writable: true,
      configurable: true,
    });

    const strategy = new OrderPDFStrategy();

    await expect(
      strategy.reviewOrder({
        comments: '',
        signed: false,
        status: OrderReviewStatus.Unapproved,
        documentData: '',
        supportingDocumentData: '',
      })
    ).rejects.toThrow('Order ID not found in URL');
  });
});
