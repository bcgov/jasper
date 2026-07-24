import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { OrderPDFStrategy } from '@/components/documents/strategies/OrderPDFStrategy';
import {
  useCommonStore,
  useOrdersStore,
  usePDFViewerStore,
  useSnackbarStore,
} from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { Order, OrderReview } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { DocumentRequestType } from '@/types/shared';
import { viewOrderSupportingDocuments } from '@/utils/orderDetails';
import { ToolbarItem } from '@nutrient-sdk/viewer';
import { createPinia, setActivePinia } from 'pinia';
import { beforeEach, describe, expect, it, Mock, vi } from 'vitest';
import { inject } from 'vue';

vi.mock('@/stores', () => ({
  usePDFViewerStore: vi.fn(),
  useSnackbarStore: vi.fn(),
  useCommonStore: vi.fn(),
  useOrdersStore: vi.fn(),
}));
vi.mock('@/utils/orderDetails', () => ({
  viewOrderSupportingDocuments: vi.fn(),
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
const mockedUseOrdersStore = useOrdersStore as unknown as Mock;
const mockedInject = inject as unknown as Mock;
const mockedViewOrderSupportingDocuments =
  viewOrderSupportingDocuments as unknown as Mock;

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

const createMockOrder = (
  id: string,
  overrides: Partial<Order> = {}
): Order => ({
  id,
  packageId: 1,
  priorityType: '',
  courtListType: '',
  packageDocumentId: '',
  packageName: '',
  receivedDate: '',
  processedDate: '',
  courtClass: '',
  courtFileNumber: 'ABC123',
  styleOfCause: '',
  physicalFileId: '',
  status: OrderReviewStatus.Unapproved,
  packageDocuments: [],
  relevantCeisDocuments: [],
  ...overrides,
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

const mockOrdersStore = {
  orders: [createMockOrder('123')],
};

const mockApiResponse: GeneratePdfResponse = {
  base64Pdf: 'base64string',
  pageRanges: [
    { start: 1, end: 2 },
    { start: 3, end: 4 },
    { start: 5, end: 5 },
  ],
};

const setLocationSearch = (search: string) => {
  Object.defineProperty(globalThis, 'location', {
    value: { search },
    writable: true,
    configurable: true,
  });
};

describe('OrderPDFStrategy', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockedUsePDFViewerStore.mockReturnValue(mockPDFViewerStore);
    mockedUseSnackbarStore.mockReturnValue(mockSnackbarStore);
    mockedUseCommonStore.mockReturnValue(mockCommonStore);
    mockedUseOrdersStore.mockReturnValue(mockOrdersStore);
    mockOrdersStore.orders = [createMockOrder('123')];
    mockedInject.mockClear();
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      if (key === 'orderService') return mockOrderService;
      return undefined;
    });

    setLocationSearch('?id=123');

    mockPDFViewerStore.hasPdfData.mockImplementation(() => true);
    mockPDFViewerStore.getPdfItems.mockImplementation(() => mockStoreDocuments);
    mockPDFViewerStore.clearPdfItems.mockClear();
    mockFilesService.generatePdf.mockClear();
    mockOrderService.review.mockClear();
    mockSnackbarStore.showSnackbar.mockClear();
    mockedViewOrderSupportingDocuments.mockClear();
  });

  it('throws if OrderService is not injected', () => {
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      return undefined;
    });

    expect(() => new OrderPDFStrategy()).toThrow('Service(s) is undefined.');
  });

  it('throws if the current order is not found in the store', () => {
    mockedUseOrdersStore.mockReturnValueOnce({ orders: [] });

    expect(() => new OrderPDFStrategy()).toThrow(
      'Current order not found in store.'
    );
  });

  it('throws if the order ID is not present in the URL', () => {
    setLocationSearch('');

    expect(() => new OrderPDFStrategy()).toThrow(
      'Current order not found in store.'
    );
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

  it('createOutline falls back to an "Order" title when the document name is empty', () => {
    const strategy = new OrderPDFStrategy();
    const documents = [createMockDocument('9', '', 'Group 1')];
    const apiResponse: GeneratePdfResponse = {
      base64Pdf: 'base64string',
      pageRanges: [{ start: 1, end: 1 }],
    };

    const outline = strategy.createOutline(documents, apiResponse);

    expect(outline[0]?.children?.[0]).toMatchObject({
      title: 'Order',
      pageIndex: 1,
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

    expect(firstOutline[0]?.pageIndex).toBe(1);
    expect(firstOutline[0]?.children?.[0]?.children?.[0]?.pageIndex).toBe(1);
    expect(secondOutline[0]?.pageIndex).toBe(1);
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
      '✅ Approved!'
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
      '📋 Rejected'
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
      '⏳ Pending'
    );
  });

  it('reviewOrder uses the resolved order ID from the store', async () => {
    mockedUseOrdersStore.mockReturnValueOnce({
      orders: [createMockOrder('order-abc')],
    });
    setLocationSearch('?id=order-abc');

    const strategy = new OrderPDFStrategy();
    const review: OrderReview = {
      comments: '',
      signed: true,
      status: OrderReviewStatus.Approved,
      documentData: '',
      supportingDocumentData: '',
    };

    await strategy.reviewOrder(review);

    expect(mockOrderService.review).toHaveBeenCalledWith('order-abc', review);
  });

  describe('additionalToolbarItems', () => {
    it('returns a supporting-documents button when the order has supporting documents', () => {
      mockedUseOrdersStore.mockReturnValueOnce({
        orders: [
          createMockOrder('123', {
            packageDocuments: [
              {
                documentId: 1,
                documentTypeCd: 'A',
                documentTypeDesc: 'Doc A',
                order: 0,
                referredDocument: false,
              },
            ],
          }),
        ],
      });

      const strategy = new OrderPDFStrategy();
      const extras = strategy.additionalToolbarItems();

      expect(extras).toHaveLength(1);
      expect(extras[0]).toMatchObject({
        type: 'custom',
        id: 'open-supporting-documents',
      });
    });

    it('returns an empty array when the order has no supporting documents', () => {
      const strategy = new OrderPDFStrategy();

      expect(strategy.additionalToolbarItems()).toEqual([]);
    });

    it('ignores referred package documents when determining supporting documents', () => {
      mockedUseOrdersStore.mockReturnValueOnce({
        orders: [
          createMockOrder('123', {
            packageDocuments: [
              {
                documentId: 1,
                documentTypeCd: 'A',
                documentTypeDesc: 'Doc A',
                order: 0,
                referredDocument: true,
              },
            ],
          }),
        ],
      });

      const strategy = new OrderPDFStrategy();

      expect(strategy.additionalToolbarItems()).toEqual([]);
    });

    it('counts relevant CEIS documents as supporting documents', () => {
      mockedUseOrdersStore.mockReturnValueOnce({
        orders: [
          createMockOrder('123', {
            relevantCeisDocuments: [
              {
                documentId: 2,
                documentTypeCd: 'B',
                documentTypeDesc: 'Doc B',
              },
            ],
          }),
        ],
      });

      const strategy = new OrderPDFStrategy();

      expect(strategy.additionalToolbarItems()).toHaveLength(1);
    });

    it('invokes viewOrderSupportingDocuments when the button is pressed', () => {
      const order = createMockOrder('123', {
        relevantCeisDocuments: [
          {
            documentId: 2,
            documentTypeCd: 'B',
            documentTypeDesc: 'Doc B',
          },
        ],
      });
      mockedUseOrdersStore.mockReturnValueOnce({ orders: [order] });

      const strategy = new OrderPDFStrategy();
      const [button] = strategy.additionalToolbarItems();

      (button as unknown as { onPress: () => void }).onPress();

      expect(mockedViewOrderSupportingDocuments).toHaveBeenCalledWith(order);
    });
  });

  describe('setToolbarItems', () => {
    it('removes note, print, callout, and image items from the toolbar', () => {
      const strategy = new OrderPDFStrategy();
      const items = [
        { type: 'pan' },
        { type: 'note' },
        { type: 'print' },
        { type: 'callout' },
        { type: 'image' },
        { type: 'zoom-in' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);

      expect(result.some((item) => item.type === 'note')).toBe(false);
      expect(result.some((item) => item.type === 'print')).toBe(false);
      expect(result.some((item) => item.type === 'callout')).toBe(false);
      expect(result.some((item) => item.type === 'pan')).toBe(true);
      expect(result.some((item) => item.type === 'zoom-in')).toBe(true);

      const panIndex = result.findIndex((item) => item.type === 'pan');
      const zoomIndex = result.findIndex((item) => item.type === 'zoom-in');
      expect(panIndex).toBeLessThan(zoomIndex);
    });

    it('inserts extras immediately after the linearized-download-indicator anchor', () => {
      const strategy = new OrderPDFStrategy();
      const items = [
        { type: 'pan' },
        { type: 'linearized-download-indicator' },
        { type: 'zoom-in' },
        { id: 'open-information', type: 'custom' },
        { id: 'open-document-review', type: 'custom' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);

      const anchorIndex = result.findIndex(
        (item) => item.type === 'linearized-download-indicator'
      );
      expect(anchorIndex).toBeGreaterThanOrEqual(0);
      expect(result[anchorIndex + 1].type).toBe('spacer');
      expect(result[anchorIndex + 2].id).toBe('open-information');
      expect(result[anchorIndex + 3].id).toBe('open-document-review');
    });

    it('appends extras at the end when no linearized-download-indicator exists', () => {
      const strategy = new OrderPDFStrategy();
      const items = [
        { type: 'pan' },
        { type: 'zoom-in' },
        { id: 'open-information', type: 'custom' },
        { id: 'open-document-review', type: 'custom' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);

      const spacerIndex = result.findIndex((item) => item.type === 'spacer');
      expect(spacerIndex).toBe(result.length - 3);
      expect(result[result.length - 2].id).toBe('open-information');
      expect(result[result.length - 1].id).toBe('open-document-review');
    });

    it('filters out missing extra items (undefined)', () => {
      const strategy = new OrderPDFStrategy();
      const items = [
        { type: 'pan' },
        { type: 'zoom-in' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);

      expect(result.some((item) => item === undefined)).toBe(false);
      expect(result.filter((item) => item.type === 'spacer').length).toBe(1);
      expect(result.some((item) => item.id === 'open-information')).toBe(false);
      expect(result.some((item) => item.id === 'open-document-review')).toBe(
        false
      );
    });

    it('moves an image item into the extras section after the anchor', () => {
      const strategy = new OrderPDFStrategy();
      const imageWithId = { id: 'custom-image', type: 'image' };
      const items = [
        { type: 'pan' },
        { type: 'linearized-download-indicator' },
        imageWithId,
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);

      const anchorIndex = result.findIndex(
        (item) => item.type === 'linearized-download-indicator'
      );
      expect(result[anchorIndex + 1].type).toBe('spacer');
      expect(result[anchorIndex + 2]).toBe(imageWithId);
    });

    it('returns only a spacer when given an empty items array', () => {
      const strategy = new OrderPDFStrategy();

      const result = strategy.setToolbarItems([] as ToolbarItem[]);

      expect(result).toEqual([{ type: 'spacer' }]);
    });

    it('preserves the relative order of non-removed base items', () => {
      const strategy = new OrderPDFStrategy();
      const items = [
        { type: 'pan' },
        { type: 'note' },
        { type: 'zoom-in' },
        { type: 'print' },
        { type: 'zoom-out' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);
      const baseTypes = result
        .filter((item) => item.type !== 'spacer')
        .map((item) => item.type);

      expect(baseTypes).toEqual(['pan', 'zoom-in', 'zoom-out']);
    });

    it('inserts extras in the expected order: spacer, open-supporting-documents, open-information, image, open-document-review', () => {
      mockedUseOrdersStore.mockReturnValueOnce({
        orders: [
          createMockOrder('123', {
            relevantCeisDocuments: [
              {
                documentId: 2,
                documentTypeCd: 'B',
                documentTypeDesc: 'Doc B',
              },
            ],
          }),
        ],
      });

      const strategy = new OrderPDFStrategy();
      const openInformation = { id: 'open-information', type: 'custom' };
      const imageItem = { type: 'image' };
      const openDocumentReview = {
        id: 'open-document-review',
        type: 'custom',
      };
      const items = [
        { type: 'linearized-download-indicator' },
        openInformation,
        imageItem,
        openDocumentReview,
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);

      const anchorIndex = result.findIndex(
        (item) => item.type === 'linearized-download-indicator'
      );
      expect(result[anchorIndex + 1].type).toBe('spacer');
      expect(result[anchorIndex + 2].id).toBe('open-supporting-documents');
      expect(result[anchorIndex + 3]).toBe(openInformation);
      expect(result[anchorIndex + 4]).toBe(imageItem);
      expect(result[anchorIndex + 5]).toBe(openDocumentReview);
    });

    it('does not add extras when viewing supporting documents', () => {
      setLocationSearch('?id=123&isSupportingDocuments=true');

      const strategy = new OrderPDFStrategy();
      const items = [
        { type: 'pan' },
        { type: 'linearized-download-indicator' },
        { id: 'open-information', type: 'custom' },
        { id: 'open-document-review', type: 'custom' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items);

      expect(result.some((item) => item.type === 'spacer')).toBe(false);
      expect(result.some((item) => item.id === 'open-information')).toBe(false);
      expect(result.some((item) => item.id === 'open-document-review')).toBe(
        false
      );
    });
  });
});
