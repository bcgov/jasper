import { Order } from '@/types';
import {
  viewOrderDetails,
  viewOrderSupportingDocuments,
} from '@/utils/orderDetails';
import { beforeEach, describe, expect, it, vi } from 'vitest';

const { openOrderDocumentsMock, getCourtClassLabelMock, isCriminalMock } =
  vi.hoisted(() => ({
    openOrderDocumentsMock: vi.fn(),
    getCourtClassLabelMock: vi.fn(),
    isCriminalMock: vi.fn(),
  }));

vi.mock('@/components/shared', () => ({
  default: {
    openOrderDocuments: openOrderDocumentsMock,
  },
}));

vi.mock('@/utils/utils', () => ({
  getCourtClassLabel: getCourtClassLabelMock,
  isCourtClassLabelCriminal: isCriminalMock,
}));

const createOrder = (overrides: Partial<Order> = {}): Order =>
  ({
    id: 'ORDER1',
    packageId: 1,
    priorityType: '',
    courtListType: '',
    packageDocumentId: '100',
    packageName: 'Package',
    receivedDate: '2024-06-01',
    processedDate: '2024-06-02',
    courtClass: 'A',
    courtFileNumber: 'FN001',
    styleOfCause: 'Style',
    physicalFileId: 'PHYS1',
    status: {} as Order['status'],
    packageDocuments: [],
    relevantCeisDocuments: [],
    ...overrides,
  }) as Order;

describe('orderDetails', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  describe('viewOrderDetails', () => {
    it('opens the referred package document with its type description', () => {
      const order = createOrder({
        packageDocuments: [
          {
            documentId: 100,
            documentTypeDesc: 'Order for Custody',
            referredDocument: true,
          },
          {
            documentId: 200,
            documentTypeDesc: 'Supporting',
            referredDocument: false,
          },
        ],
      });
      getCourtClassLabelMock.mockReturnValue('Criminal');
      isCriminalMock.mockReturnValue(true);

      viewOrderDetails(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001',
        [
          {
            courtClass: 'A',
            fileId: 'PHYS1',
            fileNumberText: 'FN001',
            isCriminal: true,
            orderId: 'ORDER1',
            documentId: '100',
            documentDescription: 'Order for Custody',
          },
        ],
        { isShowingSupportingDocs: 'false' }
      );
    });

    it('leaves the document description undefined when no referred document matches', () => {
      const order = createOrder({
        courtClass: 'F',
        packageDocuments: [
          {
            documentId: 100,
            documentTypeDesc: 'Order for Custody',
            referredDocument: false,
          },
        ],
      });
      getCourtClassLabelMock.mockReturnValue('Civil');
      isCriminalMock.mockReturnValue(false);

      viewOrderDetails(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001',
        [
          {
            courtClass: 'F',
            fileId: 'PHYS1',
            fileNumberText: 'FN001',
            isCriminal: false,
            orderId: 'ORDER1',
            documentId: '100',
            documentDescription: undefined,
          },
        ],
        { isShowingSupportingDocs: 'false' }
      );
    });

    it('matches package documents by string-coerced document id', () => {
      const order = createOrder({
        packageDocuments: [
          {
            documentId: 100,
            documentTypeDesc: 'Matched By Coercion',
            referredDocument: true,
          },
        ],
      });

      viewOrderDetails(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001',
        expect.arrayContaining([
          expect.objectContaining({
            documentDescription: 'Matched By Coercion',
          }),
        ]),
        { isShowingSupportingDocs: 'false' }
      );
    });

    it('maps order fields and opens order document for criminal files', () => {
      const order = createOrder({
        id: 'order-1',
        packageDocumentId: 'doc-1',
        packageName: 'Order package',
        courtClass: 'CC',
        courtFileNumber: 'CF-1234',
        physicalFileId: 'file-1',
        packageDocuments: [
          {
            documentId: 1,
            documentTypeDesc: 'Order package',
            referredDocument: true,
          },
        ],
      });
      getCourtClassLabelMock.mockReturnValue('Criminal');
      isCriminalMock.mockReturnValue(true);

      viewOrderDetails(order);

      expect(getCourtClassLabelMock).toHaveBeenCalledWith('CC');
      expect(isCriminalMock).toHaveBeenCalledWith('Criminal');
      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'order-1',
        'CF-1234',
        expect.arrayContaining([
          expect.objectContaining({
            courtClass: 'CC',
            fileId: 'file-1',
            fileNumberText: 'CF-1234',
            documentId: 'doc-1',
            documentDescription: 'Order package',
            isCriminal: true,
            orderId: 'order-1',
          }),
        ]),
        { isShowingSupportingDocs: 'false' }
      );
    });

    it('sets isCriminal to false for non-criminal files', () => {
      const order = createOrder({
        id: 'order-2',
        courtListType: 'Civil List',
        packageDocumentId: 'doc-2',
        packageName: 'Civil order package',
        courtClass: 'CV',
        courtFileNumber: 'CV-4321',
        physicalFileId: 'file-2',
      });
      getCourtClassLabelMock.mockReturnValue('Civil');
      isCriminalMock.mockReturnValue(false);

      viewOrderDetails(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'order-2',
        'CV-4321',
        expect.arrayContaining([
          expect.objectContaining({
            isCriminal: false,
            orderId: 'order-2',
          }),
        ]),
        { isShowingSupportingDocs: 'false' }
      );
    });
  });

  describe('viewOrderSupportingDocuments', () => {
    it('combines non-referred package documents and relevant CEIS documents', () => {
      const order = createOrder({
        courtClass: 'F',
        packageDocuments: [
          {
            documentId: 100,
            documentTypeDesc: 'Referred Order',
            referredDocument: true,
          },
          {
            documentId: 200,
            documentTypeDesc: 'Supporting Doc',
            referredDocument: false,
          },
        ],
        relevantCeisDocuments: [
          {
            civilDocumentId: 300,
            documentTypeDesc: 'CEIS Doc',
          },
        ],
      });
      getCourtClassLabelMock.mockReturnValue('Civil');
      isCriminalMock.mockReturnValue(false);

      viewOrderSupportingDocuments(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001 - Supporting Documents',
        [
          {
            courtClass: 'F',
            fileId: 'PHYS1',
            fileNumberText: 'FN001',
            isCriminal: false,
            orderId: 'ORDER1',
            documentId: '200',
            documentDescription: 'Supporting Doc',
          },
          {
            courtClass: 'F',
            fileId: 'PHYS1',
            fileNumberText: 'FN001',
            isCriminal: false,
            orderId: 'ORDER1',
            documentId: '300',
            documentDescription: 'CEIS Doc',
          },
        ],
        { isShowingSupportingDocs: 'true' }
      );
    });

    it('excludes referred package documents from supporting documents', () => {
      const order = createOrder({
        packageDocuments: [
          {
            documentId: 100,
            documentTypeDesc: 'Referred Order',
            referredDocument: true,
          },
        ],
      });

      viewOrderSupportingDocuments(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001 - Supporting Documents',
        [],
        { isShowingSupportingDocs: 'true' }
      );
    });

    it('passes an empty array when there are no supporting documents', () => {
      viewOrderSupportingDocuments(createOrder());

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001 - Supporting Documents',
        [],
        { isShowingSupportingDocs: 'true' }
      );
    });

    it('marks criminal court classes as criminal in the base document data', () => {
      const order = createOrder({
        courtClass: 'Y',
        relevantCeisDocuments: [
          {
            civilDocumentId: 300,
            documentTypeDesc: 'CEIS Doc',
          },
        ],
      });
      getCourtClassLabelMock.mockReturnValue('Criminal');
      isCriminalMock.mockReturnValue(true);

      viewOrderSupportingDocuments(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001 - Supporting Documents',
        [
          expect.objectContaining({
            courtClass: 'Y',
            isCriminal: true,
          }),
        ],
        { isShowingSupportingDocs: 'true' }
      );
    });
  });
});
