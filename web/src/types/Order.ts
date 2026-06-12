import { OrderReviewStatus } from './common';

export interface Order {
  id: string;
  packageId: number;
  priorityType: string;
  priorityTypeDesc?: string;
  courtListType: string;
  courtListTypeDescription?: string;
  packageDocumentId: string;
  packageName: string;
  receivedDate: string;
  processedDate: string;
  courtClass: string;
  courtFileNumber: string;
  styleOfCause: string;
  physicalFileId: string;
  status: OrderReviewStatus;
}
