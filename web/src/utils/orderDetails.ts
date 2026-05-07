import shared from '@/components/shared';
import { Order } from '@/types';
import { DocumentData } from '@/types/shared';
import { getCourtClassLabel, isCourtClassLabelCriminal } from '@/utils/utils';

export const viewOrderDetails = (order: Order): void => {
  const courtClassLabel = getCourtClassLabel(order.courtClass);
  const isCriminal = isCourtClassLabelCriminal(courtClassLabel);
  const documentData: DocumentData = {
    courtClass: order.courtClass,
    fileId: order.physicalFileId,
    fileNumberText: order.courtFileNumber,
    documentId: order.packageDocumentId,
    documentDescription: order.packageName,
    isCriminal,
    orderId: order.id,
  };

  shared.openOrderDocuments(documentData);
};
