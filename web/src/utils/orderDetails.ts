import shared from '@/components/shared';
import { Order } from '@/types';
import { DocumentData } from '@/types/shared';
import { getCourtClassLabel, isCourtClassLabelCriminal } from '@/utils/utils';

export const viewOrderDetails = (order: Order): void => {
  const documentData = getBaseDocumentData(order);
  const referredDocument = (order.packageDocuments ?? []).find(
    (pd) =>
      pd.referredDocument &&
      pd.documentId?.toString() === order.packageDocumentId.toString()
  );
  const hasSupportingDocuments =
    [
      ...(order.packageDocuments ?? []).filter((pd) => !pd.referredDocument),
      ...(order.relevantCeisDocuments ?? []),
    ].length > 0;
  shared.openOrderDocuments(
    order.id,
    order.courtFileNumber,
    [
      {
        ...documentData,
        documentId: order.packageDocumentId,
        documentDescription: referredDocument?.documentTypeDesc,
      },
    ],
    hasSupportingDocuments,
    false
  );
};

export const viewOrderSupportingDocuments = async (
  order: Order
): Promise<void> => {
  const baseDocumentData = getBaseDocumentData(order);
  const packageDocs = (order.packageDocuments ?? []).filter(
    (pd) => !pd.referredDocument && pd.documentId
  );
  const ceisDocs = (order.relevantCeisDocuments ?? []).filter(
    (cd) => cd.civilDocumentId
  );

  const supportingDocumentsData: DocumentData[] = [
    ...packageDocs.map((doc) => ({
      ...baseDocumentData,
      documentId: doc.documentId?.toString(),
      documentDescription: doc.documentTypeDesc,
    })),
    ...ceisDocs.map((doc) => ({
      ...baseDocumentData,
      documentId: doc.civilDocumentId?.toString(),
      documentDescription: doc.documentTypeDesc,
    })),
  ];
  shared.openOrderDocuments(
    order.id,
    `${order.courtFileNumber} - Supporting Documents`,
    supportingDocumentsData,
    false,
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
