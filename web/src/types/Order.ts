import { OrderReviewStatus } from './common';

export interface Order {
  id: string;
  packageId: number;
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
