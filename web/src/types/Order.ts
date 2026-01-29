import { OrderStatusEnum } from './common';

export interface Order {
  id: string;
  packageNumber: number;
  receivedDate: string;
  processedDate: string;
  courtClass: string;
  courtFileNumber: string;
  styleOfCause: string;
  physicalFileId: string;
  status: OrderStatusEnum;
}
