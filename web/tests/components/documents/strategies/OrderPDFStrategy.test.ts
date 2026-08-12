import { GeneratePdfResponse } from '@/components/documents/models/GeneratePdf';
import { OrderPDFStrategy } from '@/components/documents/strategies/OrderPDFStrategy';
import { PDFViewerToolbarContext } from '@/components/documents/strategies/PDFViewerTypes';
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
import { inject, nextTick, reactive } from 'vue';

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
  hasSupportingDocs: false,
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
  getOrder: vi.fn(),
};

const mockUserService = {
  getSignature: vi.fn(),
  getInitials: vi.fn(),
};

const mockSnackbarStore = {
  showSnackbar: vi.fn(),
};

const mockCommonStore = {
  userInfo: { judgeId: 11, hasSignature: false, hasInitials: false },
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

const createMockContext = (
  overrides: Partial<PDFViewerToolbarContext> = {}
): PDFViewerToolbarContext => ({
  instance: {
    viewState: { currentPageIndex: 0 },
    createAttachment: vi.fn().mockResolvedValue('attachment-id'),
    pageInfoForIndex: vi.fn().mockReturnValue({ width: 600, height: 800 }),
    create: vi.fn().mockResolvedValue([{ id: 'annotation-id' }]),
    setSelectedAnnotations: vi.fn(),
  },
  nutrientViewer: {
    Annotations: {
      ImageAnnotation: vi.fn().mockImplementation(function (config) {
        return { ...config };
      }),
    },
    Geometry: {
      Rect: vi.fn().mockImplementation(function (config) {
        return { ...config };
      }),
    },
    Immutable: {
      List: vi.fn().mockImplementation((arr) => arr),
    },
  },
  rawData: mockStoreDocuments,
  resolveInformationContext: vi.fn(),
  openReviewModal: vi.fn(),
  updateCanApprove: vi.fn().mockResolvedValue(undefined),
  ...overrides,
});

const createOrderPDFStrategyForAnotherJudge = (): OrderPDFStrategy => {
  mockedUseCommonStore.mockReturnValueOnce({
    userInfo: { judgeId: 11, hasSignature: false, hasInitials: false },
    loggedInUserInfo: { judgeId: 12 },
  });
  return new OrderPDFStrategy();
};

describe('OrderPDFStrategy', () => {
  beforeEach(() => {
    setActivePinia(createPinia());
    mockedUsePDFViewerStore.mockReturnValue(mockPDFViewerStore);
    mockedUseSnackbarStore.mockReturnValue(mockSnackbarStore);
    mockedUseCommonStore.mockReturnValue(mockCommonStore);
    mockedUseOrdersStore.mockReturnValue(mockOrdersStore);
    mockCommonStore.userInfo = {
      judgeId: 11,
      hasSignature: false,
      hasInitials: false,
    };
    mockCommonStore.loggedInUserInfo = { judgeId: 11 };
    mockOrdersStore.orders = [createMockOrder('123')];
    mockedInject.mockClear();
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      if (key === 'orderService') return mockOrderService;
      if (key === 'userService') return mockUserService;
      return undefined;
    });

    setLocationSearch('?id=123');

    mockPDFViewerStore.hasPdfData.mockImplementation(() => true);
    mockPDFViewerStore.getPdfItems.mockImplementation(() => mockStoreDocuments);
    mockPDFViewerStore.clearPdfItems.mockClear();
    mockFilesService.generatePdf.mockClear();
    mockOrderService.review.mockClear();
    mockOrderService.getOrder.mockReset();
    mockUserService.getSignature.mockReset();
    mockUserService.getInitials.mockReset();
    mockSnackbarStore.showSnackbar.mockClear();
    mockedViewOrderSupportingDocuments.mockClear();
  });

  it('throws if OrderService is not injected', () => {
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      if (key === 'userService') return mockUserService;
      return undefined;
    });

    expect(() => new OrderPDFStrategy()).toThrow('Service(s) is undefined.');
  });

  it('throws if UserService is not injected', () => {
    mockedInject.mockImplementation((key: string) => {
      if (key === 'filesService') return mockFilesService;
      if (key === 'orderService') return mockOrderService;
      return undefined;
    });

    expect(() => new OrderPDFStrategy()).toThrow('Service(s) is undefined.');
  });

  it('throws if FilesService is not injected', () => {
    mockedInject.mockImplementation((key: string) => {
      if (key === 'orderService') return mockOrderService;
      if (key === 'userService') return mockUserService;
      return undefined;
    });

    expect(() => new OrderPDFStrategy()).toThrow(
      'FilesService is not available!'
    );
  });

  it('throws if the order ID is not present in the URL', () => {
    setLocationSearch('');

    expect(() => new OrderPDFStrategy()).toThrow(
      'Order ID is not defined in the URL parameters.'
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

  it('cleanup stops watching for judge ID changes', async () => {
    const commonStore = reactive({
      userInfo: { judgeId: 11 },
      loggedInUserInfo: { judgeId: 11 },
    });
    mockedUseCommonStore.mockReturnValueOnce(commonStore);
    mockOrderService.getOrder.mockResolvedValue(createMockOrder('123'));

    const strategy = new OrderPDFStrategy();
    strategy.cleanup();

    commonStore.userInfo.judgeId = 99;
    await nextTick();
    await strategy.initialize();

    expect(mockOrderService.getOrder).toHaveBeenCalledWith('123', 11);
  });
  describe('getRequiredApprovalAnnotations', () => {
    it('returns Signature and Initials when the user has both', () => {
      mockCommonStore.userInfo = {
        judgeId: 11,
        hasSignature: true,
        hasInitials: true,
      };

      const strategy = new OrderPDFStrategy();

      expect(strategy.getRequiredApprovalAnnotations()).toEqual([
        'Signature',
        'Initials',
      ]);
    });

    it('returns only Signature when the user has a signature but no initials', () => {
      mockCommonStore.userInfo = {
        judgeId: 11,
        hasSignature: true,
        hasInitials: false,
      };

      const strategy = new OrderPDFStrategy();

      expect(strategy.getRequiredApprovalAnnotations()).toEqual(['Signature']);
    });

    it('returns only Initials when the user has initials but no signature', () => {
      mockCommonStore.userInfo = {
        judgeId: 11,
        hasSignature: false,
        hasInitials: true,
      };

      const strategy = new OrderPDFStrategy();

      expect(strategy.getRequiredApprovalAnnotations()).toEqual(['Initials']);
    });

    it('returns undefined when the user has neither a signature nor initials', () => {
      const strategy = new OrderPDFStrategy();

      expect(strategy.getRequiredApprovalAnnotations()).toBeUndefined();
    });
  });

  describe('reviewOrder', () => {
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
        'success',
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
        'success',
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
        'success',
        '⏳ Pending'
      );
    });

    it('uses the order ID from the URL parameters', async () => {
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
  });

  describe('initialize', () => {
    it('fetches the order and stores it as the current order', async () => {
      const order = createMockOrder('123', { hasSupportingDocs: true });
      mockOrderService.getOrder.mockResolvedValue(order);

      const strategy = new OrderPDFStrategy();
      await strategy.initialize();

      expect(mockOrderService.getOrder).toHaveBeenCalledWith('123');
      // Supporting-documents button only appears once the order is loaded.
      const items = strategy.addCustomToolbarItems(createMockContext());
      expect(
        items.some((item) => item.id === 'open-supporting-documents')
      ).toBe(true);
      expect(mockOrderService.getOrder).toHaveBeenCalledWith('123', 11);
    });

    it('passes the updated judge ID to getOrder after the judge changes', async () => {
      const commonStore = reactive({
        userInfo: { judgeId: 11 },
        loggedInUserInfo: { judgeId: 11 },
      });
      mockedUseCommonStore.mockReturnValueOnce(commonStore);
      mockOrderService.getOrder.mockResolvedValue(createMockOrder('123'));

      const strategy = new OrderPDFStrategy();
      commonStore.userInfo.judgeId = 42;
      await nextTick();
      await strategy.initialize();

      expect(mockOrderService.getOrder).toHaveBeenCalledWith('123', 42);
    });

    it('throws when the order cannot be found', async () => {
      mockOrderService.getOrder.mockResolvedValue(undefined);

      const strategy = new OrderPDFStrategy();

      await expect(strategy.initialize()).rejects.toThrow(
        'Order with ID 123 not found.'
      );
    });
  });

  describe('addCustomToolbarItems', () => {
    it('returns the review options when the current user is the assigned judge', () => {
      const strategy = new OrderPDFStrategy();

      const items = strategy.addCustomToolbarItems(createMockContext());

      expect(items.map((item) => item.id)).toEqual([
        'open-information',
        'open-document-review',
      ]);
    });

    it('returns an empty array when the current user is not the assigned judge', () => {
      const strategy = createOrderPDFStrategyForAnotherJudge();

      expect(strategy.addCustomToolbarItems(createMockContext())).toEqual([]);
    });

    it('includes an add-signature item when the user has a signature', () => {
      mockCommonStore.userInfo = {
        judgeId: 11,
        hasSignature: true,
        hasInitials: false,
      };

      const strategy = new OrderPDFStrategy();
      const items = strategy.addCustomToolbarItems(createMockContext());

      expect(items.some((item) => item.id === 'add-signature')).toBe(true);
      expect(items.some((item) => item.id === 'add-initials')).toBe(false);
    });

    it('includes an add-initials item when the user has initials', () => {
      mockCommonStore.userInfo = {
        judgeId: 11,
        hasSignature: false,
        hasInitials: true,
      };

      const strategy = new OrderPDFStrategy();
      const items = strategy.addCustomToolbarItems(createMockContext());

      expect(items.some((item) => item.id === 'add-initials')).toBe(true);
      expect(items.some((item) => item.id === 'add-signature')).toBe(false);
    });

    it('does not include a supporting-documents item before the order is initialized', () => {
      const strategy = new OrderPDFStrategy();

      const items = strategy.addCustomToolbarItems(createMockContext());

      expect(
        items.some((item) => item.id === 'open-supporting-documents')
      ).toBe(false);
    });

    it('does not include a supporting-documents item when the order has none', async () => {
      mockOrderService.getOrder.mockResolvedValue(
        createMockOrder('123', { hasSupportingDocs: false })
      );

      const strategy = new OrderPDFStrategy();
      await strategy.initialize();

      const items = strategy.addCustomToolbarItems(createMockContext());
      expect(
        items.some((item) => item.id === 'open-supporting-documents')
      ).toBe(false);
    });

    it('open-information opens a civil-file window for a civil case', () => {
      const context = createMockContext({
        resolveInformationContext: vi.fn(() => ({
          physicalFileId: 'PF-1',
          isCriminal: false,
        })),
      });
      const openSpy = vi.spyOn(window, 'open').mockReturnValue(null);

      const strategy = new OrderPDFStrategy();
      const item = strategy
        .addCustomToolbarItems(context)
        .find((toolbarItem) => toolbarItem.id === 'open-information');

      (item as unknown as { onPress: () => void }).onPress();

      expect(context.resolveInformationContext).toHaveBeenCalledWith(
        context.rawData
      );
      expect(openSpy).toHaveBeenCalledWith(
        'civil-file/PF-1',
        'relatedCaseInfo'
      );

      openSpy.mockRestore();
    });

    it('open-information opens a criminal-file window for a criminal case', () => {
      const context = createMockContext({
        resolveInformationContext: vi.fn(() => ({
          physicalFileId: 'PF-2',
          isCriminal: true,
        })),
      });
      const openSpy = vi.spyOn(window, 'open').mockReturnValue(null);

      const strategy = new OrderPDFStrategy();
      const item = strategy
        .addCustomToolbarItems(context)
        .find((toolbarItem) => toolbarItem.id === 'open-information');

      (item as unknown as { onPress: () => void }).onPress();

      expect(openSpy).toHaveBeenCalledWith(
        'criminal-file/PF-2',
        'relatedCaseInfo'
      );

      openSpy.mockRestore();
    });

    it('open-information warns and does not open a window when the context is unresolved', () => {
      const context = createMockContext({
        resolveInformationContext: vi.fn(() => undefined),
      });
      const openSpy = vi.spyOn(window, 'open').mockReturnValue(null);
      const warnSpy = vi.spyOn(console, 'warn').mockImplementation(() => {});

      const strategy = new OrderPDFStrategy();
      const item = strategy
        .addCustomToolbarItems(context)
        .find((toolbarItem) => toolbarItem.id === 'open-information');

      (item as unknown as { onPress: () => void }).onPress();

      expect(openSpy).not.toHaveBeenCalled();
      expect(warnSpy).toHaveBeenCalledWith(
        'Unable to resolve PDF viewer information context.'
      );

      openSpy.mockRestore();
      warnSpy.mockRestore();
    });

    it('open-document-review opens the review modal', () => {
      const context = createMockContext();

      const strategy = new OrderPDFStrategy();
      const item = strategy
        .addCustomToolbarItems(context)
        .find((toolbarItem) => toolbarItem.id === 'open-document-review');

      (item as unknown as { onPress: () => void }).onPress();

      expect(context.openReviewModal).toHaveBeenCalled();
    });

    it('open-supporting-documents invokes viewOrderSupportingDocuments with the current order', async () => {
      const order = createMockOrder('123', { hasSupportingDocs: true });
      mockOrderService.getOrder.mockResolvedValue(order);

      const strategy = new OrderPDFStrategy();
      await strategy.initialize();
      const item = strategy
        .addCustomToolbarItems(createMockContext())
        .find((toolbarItem) => toolbarItem.id === 'open-supporting-documents');

      (item as unknown as { onPress: () => void }).onPress();

      expect(mockOrderService.getOrder).toHaveBeenCalledWith('123', 11);
      expect(mockedViewOrderSupportingDocuments).toHaveBeenCalledWith(order);
    });

    it('add-signature fetches the signature and adds a selected image annotation', async () => {
      mockCommonStore.userInfo = {
        judgeId: 11,
        hasSignature: true,
        hasInitials: false,
      };
      const blob = new Blob(['signature'], { type: 'image/png' });
      mockUserService.getSignature.mockResolvedValue(blob);
      const context = createMockContext();

      const strategy = new OrderPDFStrategy();
      const item = strategy
        .addCustomToolbarItems(context)
        .find((toolbarItem) => toolbarItem.id === 'add-signature');

      await (item as unknown as { onPress: () => Promise<void> }).onPress();

      expect(mockUserService.getSignature).toHaveBeenCalled();
      expect(context.instance.createAttachment).toHaveBeenCalledWith(blob);
      expect(context.instance.create).toHaveBeenCalled();
      expect(context.instance.setSelectedAnnotations).toHaveBeenCalled();
      expect(
        context.nutrientViewer.Annotations.ImageAnnotation
      ).toHaveBeenCalledWith(
        expect.objectContaining({
          pageIndex: 0,
          contentType: 'image/png',
          imageAttachmentId: 'attachment-id',
          description: 'Signature',
        })
      );
    });

    it('add-initials logs an error when adding the annotation fails', async () => {
      mockCommonStore.userInfo = {
        judgeId: 11,
        hasSignature: false,
        hasInitials: true,
      };
      mockUserService.getInitials.mockRejectedValue(new Error('boom'));
      const errorSpy = vi.spyOn(console, 'error').mockImplementation(() => {});
      const context = createMockContext();

      const strategy = new OrderPDFStrategy();
      const item = strategy
        .addCustomToolbarItems(context)
        .find((toolbarItem) => toolbarItem.id === 'add-initials');

      await (item as unknown as { onPress: () => Promise<void> }).onPress();

      expect(errorSpy).toHaveBeenCalledWith(
        'Failed to add initials:',
        expect.any(Error)
      );
      expect(context.instance.create).not.toHaveBeenCalled();

      errorSpy.mockRestore();
    });
  });

  describe('setToolbarItems', () => {
    it('removes note, print, callout, and image items from the toolbar', () => {
      const strategy = createOrderPDFStrategyForAnotherJudge();
      const items = [
        { type: 'pan' },
        { type: 'note' },
        { type: 'print' },
        { type: 'callout' },
        { type: 'image' },
        { type: 'zoom-in' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());

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
      const strategy = createOrderPDFStrategyForAnotherJudge();
      const items = [
        { type: 'pan' },
        { type: 'linearized-download-indicator' },
        { type: 'zoom-in' },
        { id: 'open-information', type: 'custom' },
        { id: 'open-document-review', type: 'custom' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());

      const anchorIndex = result.findIndex(
        (item) => item.type === 'linearized-download-indicator'
      );
      expect(anchorIndex).toBeGreaterThanOrEqual(0);
      expect(result[anchorIndex + 1].type).toBe('spacer');
      expect(result[anchorIndex + 2].id).toBe('open-information');
      expect(result[anchorIndex + 3].id).toBe('open-document-review');
    });

    it('appends extras at the end when no linearized-download-indicator exists', () => {
      const strategy = createOrderPDFStrategyForAnotherJudge();
      const items = [
        { type: 'pan' },
        { type: 'zoom-in' },
        { id: 'open-information', type: 'custom' },
        { id: 'open-document-review', type: 'custom' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());

      const spacerIndex = result.findIndex((item) => item.type === 'spacer');
      expect(spacerIndex).toBe(result.length - 3);
      expect(result[result.length - 2].id).toBe('open-information');
      expect(result[result.length - 1].id).toBe('open-document-review');
    });

    it('filters out missing extra items (undefined)', () => {
      const strategy = createOrderPDFStrategyForAnotherJudge();
      const items = [
        { type: 'pan' },
        { type: 'zoom-in' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());

      expect(result.some((item) => item === undefined)).toBe(false);
      expect(result.filter((item) => item.type === 'spacer').length).toBe(1);
      expect(result.some((item) => item.id === 'open-information')).toBe(false);
      expect(result.some((item) => item.id === 'open-document-review')).toBe(
        false
      );
    });

    it('moves an image item into the extras section after the anchor', () => {
      const strategy = createOrderPDFStrategyForAnotherJudge();
      const imageWithId = { id: 'custom-image', type: 'image' };
      const items = [
        { type: 'pan' },
        { type: 'linearized-download-indicator' },
        imageWithId,
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());

      const anchorIndex = result.findIndex(
        (item) => item.type === 'linearized-download-indicator'
      );
      expect(result[anchorIndex + 1].type).toBe('spacer');
      expect(result[anchorIndex + 2]).toBe(imageWithId);
    });

    it('returns only a spacer when given an empty items array for a non-reviewer', () => {
      const strategy = createOrderPDFStrategyForAnotherJudge();

      const result = strategy.setToolbarItems(
        [] as ToolbarItem[],
        createMockContext()
      );

      expect(result).toEqual([{ type: 'spacer' }]);
    });

    it('preserves the relative order of non-removed base items', () => {
      const strategy = createOrderPDFStrategyForAnotherJudge();
      const items = [
        { type: 'pan' },
        { type: 'note' },
        { type: 'zoom-in' },
        { type: 'print' },
        { type: 'zoom-out' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());
      const baseTypes = result
        .filter((item) => item.type !== 'spacer')
        .map((item) => item.type);

      expect(baseTypes).toEqual(['pan', 'zoom-in', 'zoom-out']);
    });

    it('inserts extras in the expected order: spacer, open-supporting-documents, open-information, image, open-document-review', async () => {
      mockOrderService.getOrder.mockResolvedValue(
        createMockOrder('123', { hasSupportingDocs: true })
      );

      const strategy = createOrderPDFStrategyForAnotherJudge();
      await strategy.initialize();
      const openSupportingDocuments = {
        id: 'open-supporting-documents',
        type: 'custom',
      };
      const openInformation = { id: 'open-information', type: 'custom' };
      const imageItem = { type: 'image' };
      const openDocumentReview = {
        id: 'open-document-review',
        type: 'custom',
      };
      const items = [
        { type: 'linearized-download-indicator' },
        openSupportingDocuments,
        openInformation,
        imageItem,
        openDocumentReview,
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());

      const anchorIndex = result.findIndex(
        (item) => item.type === 'linearized-download-indicator'
      );
      expect(result[anchorIndex + 1].type).toBe('spacer');
      expect(result[anchorIndex + 2]).toBe(openSupportingDocuments);
      expect(result[anchorIndex + 3]).toBe(openInformation);
      expect(result[anchorIndex + 4]).toBe(imageItem);
      expect(result[anchorIndex + 5]).toBe(openDocumentReview);
    });

    it('does not add extras when viewing supporting documents', () => {
      setLocationSearch('?id=123&isShowingSupportingDocs=true');

      const strategy = createOrderPDFStrategyForAnotherJudge();
      const items = [
        { type: 'pan' },
        { type: 'linearized-download-indicator' },
        { id: 'open-information', type: 'custom' },
        { id: 'open-document-review', type: 'custom' },
      ] as unknown as ToolbarItem[];

      const result = strategy.setToolbarItems(items, createMockContext());

      expect(result.some((item) => item.type === 'spacer')).toBe(false);
      expect(result.some((item) => item.id === 'open-information')).toBe(false);
      expect(result.some((item) => item.id === 'open-document-review')).toBe(
        false
      );
    });
  });
});
