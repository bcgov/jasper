import { OrderReviewStatus } from './common';

export interface OrderReview {
  comments: string;
  signed: boolean;
  status: OrderReviewStatus;
  documentData: Blob | null;
}
