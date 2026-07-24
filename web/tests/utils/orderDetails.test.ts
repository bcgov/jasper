import shared from '@/components/shared';
import { Order } from '@/types';
import {
  viewOrderDetails,
  viewOrderSupportingDocuments,
} from '@/utils/orderDetails';
import { beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('@/components/shared', () => ({
  default: {
    openOrderDocuments: vi.fn(),
  },
}));

const openOrderDocumentsMock = vi.mocked(shared.openOrderDocuments);

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
        packageDocumentId: '100',
        packageDocuments: [
          {
            documentId: 100,
            documentTypeCd: 'ORD',
            documentTypeDesc: 'Order for Custody',
            order: 1,
            referredDocument: true,
          },
          {
            documentId: 200,
            documentTypeCd: 'SUP',
            documentTypeDesc: 'Supporting',
            order: 2,
            referredDocument: false,
          },
        ],
      });

      viewOrderDetails(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith('ORDER1', 'FN001', [
        {
          courtClass: 'A',
          fileId: 'PHYS1',
          fileNumberText: 'FN001',
          isCriminal: true,
          orderId: 'ORDER1',
          documentId: '100',
          documentDescription: 'Order for Custody',
        },
      ]);
    });

    it('leaves the document description undefined when no referred document matches', () => {
      const order = createOrder({
        packageDocumentId: '100',
        courtClass: 'F',
        packageDocuments: [
          {
            documentId: 100,
            documentTypeCd: 'ORD',
            documentTypeDesc: 'Order for Custody',
            order: 1,
            referredDocument: false,
          },
        ],
      });

      viewOrderDetails(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith('ORDER1', 'FN001', [
        {
          courtClass: 'F',
          fileId: 'PHYS1',
          fileNumberText: 'FN001',
          isCriminal: false,
          orderId: 'ORDER1',
          documentId: '100',
          documentDescription: undefined,
        },
      ]);
    });

    it('matches package documents by string-coerced document id', () => {
      const order = createOrder({
        packageDocumentId: '100',
        packageDocuments: [
          {
            documentId: 100,
            documentTypeCd: 'ORD',
            documentTypeDesc: 'Matched By Coercion',
            order: 1,
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
        ])
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
            documentTypeCd: 'ORD',
            documentTypeDesc: 'Referred Order',
            order: 1,
            referredDocument: true,
          },
          {
            documentId: 200,
            documentTypeCd: 'SUP',
            documentTypeDesc: 'Supporting Doc',
            order: 2,
            referredDocument: false,
          },
        ],
        relevantCeisDocuments: [
          {
            documentId: 300,
            documentTypeCd: 'CEIS',
            documentTypeDesc: 'CEIS Doc',
          },
        ],
      });

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
        true
      );
    });

    it('excludes referred package documents from supporting documents', () => {
      const order = createOrder({
        packageDocuments: [
          {
            documentId: 100,
            documentTypeCd: 'ORD',
            documentTypeDesc: 'Referred Order',
            order: 1,
            referredDocument: true,
          },
        ],
        relevantCeisDocuments: [],
      });

      viewOrderSupportingDocuments(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001 - Supporting Documents',
        [],
        true
      );
    });

    it('passes an empty array when there are no supporting documents', () => {
      const order = createOrder({
        packageDocuments: [],
        relevantCeisDocuments: [],
      });

      viewOrderSupportingDocuments(order);

      expect(openOrderDocumentsMock).toHaveBeenCalledWith(
        'ORDER1',
        'FN001 - Supporting Documents',
        [],
        true
      );
    });

    it('marks criminal court classes as criminal in the base document data', () => {
      const order = createOrder({
        courtClass: 'Y',
        relevantCeisDocuments: [
          {
            documentId: 300,
            documentTypeCd: 'CEIS',
            documentTypeDesc: 'CEIS Doc',
          },
        ],
      });

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
        true
      );
    });
  });
});
