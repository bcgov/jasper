import shared from '@/components/shared';
import { Order } from '@/types';
import { DocumentData } from '@/types/shared';
import { getCourtClassLabel, isCourtClassLabelCriminal } from '@/utils/utils';

export const viewOrderDetails = (order: Order): void => {
  const documentData = getBaseDocumentData(order);
  const referredDocument = order.packageDocuments.find(
    (pd) =>
      pd.documentId.toString() === order.packageDocumentId.toString() &&
      pd.referredDocument
  );
  shared.openOrderDocuments(order.id, order.courtFileNumber, [
    {
      ...documentData,
      documentId: order.packageDocumentId,
      documentDescription: referredDocument?.documentTypeDesc,
    },
  ]);
};

export const viewOrderSupportingDocuments = (order: Order): void => {
  const baseDocumentData = getBaseDocumentData(order);
  const allDocuments = [
    ...order.packageDocuments.filter((pd) => !pd.referredDocument),
    ...order.relevantCeisDocuments,
  ];

  const supportingDocumentsData: DocumentData[] = allDocuments.map((doc) => ({
    ...baseDocumentData,
    documentId: doc.documentId.toString(),
    documentDescription: doc.documentTypeDesc,
  }));

  shared.openOrderDocuments(
    order.id,
    `${order.courtFileNumber} - Supporting Documents`,
    supportingDocumentsData,
    true
  );
};

const getBaseDocumentData = (order: Order): DocumentData => {
  const courtClassLabel = getCourtClassLabel(order.courtClass);
  const isCriminal = isCourtClassLabelCriminal(courtClassLabel);
  return {
    courtClass: order.courtClass,
    fileId: order.physicalFileId,
    fileNumberText: order.courtFileNumber,
    isCriminal,
    orderId: order.id,
  };
};
