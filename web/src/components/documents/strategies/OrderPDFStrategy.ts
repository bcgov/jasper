import { OrderService } from '@/services';
import { useCommonStore, useSnackbarStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { OrderReview } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { inject } from 'vue';
import { FilePDFStrategy } from './FilePDFStrategy';

export class OrderPDFStrategy extends FilePDFStrategy {
  showOrderReviewOptions = true;

  private readonly snackBarStore = useSnackbarStore();
  private readonly commonStore = useCommonStore();
  private readonly orderService: OrderService;

  constructor() {
    super();

    const orderService = inject<OrderService>('orderService');

    if (!orderService) {
      throw new Error('Service(s) is undefined.');
    }

    this.orderService = orderService;
    this.showOrderReviewOptions =
      this.commonStore.userInfo?.judgeId ===
      this.commonStore.loggedInUserInfo?.judgeId;
  }

  protected override getOutlineDocumentTitle(document: StoreDocument): string {
    return document.documentName || 'Order';
  }

  async reviewOrder(review: OrderReview): Promise<void> {
    const urlParams = new URLSearchParams(globalThis.location.search);
    const orderId = urlParams.get('id');

    if (!orderId) {
      throw new Error('Order ID not found in URL');
    }

    await this.orderService.review(orderId, review);

    switch (review.status) {
      case OrderReviewStatus.Approved:
        this.snackBarStore.showSnackbar(
          'The order has been approved.',
          'rgb(46, 139, 43)',
          '✅ Approved!'
        );
        break;
      case OrderReviewStatus.Unapproved:
        this.snackBarStore.showSnackbar(
          'The order has been rejected.',
          'rgb(46, 139, 43)',
          '📋 Rejected'
        );
        break;
      case OrderReviewStatus.Pending:
      case OrderReviewStatus.AwaitingDocumentation:
        this.snackBarStore.showSnackbar(
          'The order review is awaiting documentation.',
          'rgb(46, 139, 43)',
          '⏳ Pending'
        );
        break;
    }
  }
}
