import { OrderReview } from '@/types/OrderReview';
import { BasePDFStrategy } from './BasePDFStrategy';
import { OrderService } from '@/services/OrderService';
import { inject } from 'vue';
import { OrderReviewStatus } from '@/types/common';
import { useSnackbarStore } from '@/stores/SnackbarStore';

export class OrderPDFStrategy extends BasePDFStrategy {
  showOrderReviewOptions = true;
  defaultDocumentName = 'Order';
  private readonly snackBarStore = useSnackbarStore();
  private readonly orderService: OrderService;

  constructor() {
    super();
    const orderService = inject<OrderService>('orderService');
    if (!orderService) {
      throw new Error('Service(s) is undefined.');
    }
    this.orderService = orderService;
  }

  async reviewOrder(review: OrderReview): Promise<void> {
    // Get order ID from URL query parameter
    const urlParams = new URLSearchParams(globalThis.location.search);
    const orderId = urlParams.get('id');
    if (!orderId) {
      throw new Error('Order ID not found in URL');
    }

    await this.orderService.review(orderId, review);

    // Show appropriate snackbar based on status
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
        this.snackBarStore.showSnackbar(
          'The order review is awaiting documentation.',
          'rgb(46, 139, 43)',
          '⏳ Pending'
        );
        break;
    }
  }
}
