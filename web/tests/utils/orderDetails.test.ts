import { OrderReviewStatus } from '@/types/common';
import { viewOrderDetails } from '@/utils/orderDetails';
import { describe, expect, it, vi } from 'vitest';

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

describe('viewOrderDetails', () => {
  it('maps order fields and opens order document for criminal files', () => {
    const order = {
      id: 'order-1',
      packageId: 1001,
      priorityType: 'P1',
      priorityTypeDescription: 'High',
      courtListType: 'Trial List',
      packageDocumentId: 'doc-1',
      packageName: 'Order package',
      receivedDate: '2026-01-01',
      processedDate: '2026-01-02',
      courtClass: 'CC',
      courtFileNumber: 'CF-1234',
      styleOfCause: 'R v Smith',
      physicalFileId: 'file-1',
      status: OrderReviewStatus.Pending,
    };
    getCourtClassLabelMock.mockReturnValue('Criminal');
    isCriminalMock.mockReturnValue(true);

    viewOrderDetails(order);

    expect(getCourtClassLabelMock).toHaveBeenCalledWith('CC');
    expect(isCriminalMock).toHaveBeenCalledWith('Criminal');
    expect(openOrderDocumentsMock).toHaveBeenCalledWith({
      courtClass: 'CC',
      fileId: 'file-1',
      fileNumberText: 'CF-1234',
      documentId: 'doc-1',
      documentDescription: 'Order package',
      isCriminal: true,
      orderId: 'order-1',
    });
  });

  it('sets isCriminal to false for non-criminal files', () => {
    const order = {
      id: 'order-2',
      packageId: 1002,
      priorityType: 'P2',
      courtListType: 'Civil List',
      packageDocumentId: 'doc-2',
      packageName: 'Civil order package',
      receivedDate: '2026-01-01',
      processedDate: '2026-01-02',
      courtClass: 'CV',
      courtFileNumber: 'CV-4321',
      styleOfCause: 'Jones v Brown',
      physicalFileId: 'file-2',
      status: OrderReviewStatus.Pending,
    };
    getCourtClassLabelMock.mockReturnValue('Civil');
    isCriminalMock.mockReturnValue(false);

    viewOrderDetails(order);

    expect(openOrderDocumentsMock).toHaveBeenCalledWith(
      expect.objectContaining({
        isCriminal: false,
        orderId: 'order-2',
      })
    );
  });
});
