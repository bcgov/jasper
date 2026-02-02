import { OrderReview } from '@/types/OrderReview';
import { BasePDFStrategy } from './BasePDFStrategy';
import { OrderService } from '@/services/OrderService';
import { inject } from 'vue';
import { OrderStatusEnum } from '@/types/common';
import { useSnackbarStore } from '@/stores/SnackbarStore';
  
export class OrderPDFStrategy extends BasePDFStrategy {
  
  showOrderReviewOptions = true;
  defaultDocumentName = 'Order';
  snackBarStore;
  private orderService: OrderService;

  constructor() {
    super();
    const orderService = inject<OrderService>('orderService');
    if (!orderService) {
      throw new Error('Service(s) is undefined.');
    }
    this.orderService = orderService;
    this.snackBarStore = useSnackbarStore();
  }

  async approveOrder(comments: string, pdfString: string): Promise<void> {
    const review: OrderReview = {
      comments,
      signed: true,
      status: OrderStatusEnum.Approved,
      documentData: pdfString
    };
        // Get order ID from URL query parameter
    const urlParams = new URLSearchParams(window.location.search);
    const id = urlParams.get('id');
    if (!id) {
      throw new Error('Order ID not found in URL');
    }
    const orderId = id;
    await this.orderService.review(orderId, review);

    
  this.snackBarStore.showSnackbar(
     'The order has been approved successfully.',
      'rgb(46, 139, 43)',
      '✅ Approved!'
    );
  }
}
