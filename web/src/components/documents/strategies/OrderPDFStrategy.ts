import { OrderService } from '@/services/OrderService';
import { useCommonStore, useSnackbarStore } from '@/stores';
import { OrderReviewStatus } from '@/types/common';
import { OrderReview } from '@/types/OrderReview';
import { ToolbarItem } from '@nutrient-sdk/viewer';
import { inject } from 'vue';
import { BasePDFStrategy } from './BasePDFStrategy';

export class OrderPDFStrategy extends BasePDFStrategy {
  showOrderReviewOptions = true;
  defaultDocumentName = 'Order';
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

    // Only show review options if the logged-in user is viewing their own data
    this.showOrderReviewOptions =
      this.commonStore.userInfo?.judgeId ===
      this.commonStore.loggedInUserInfo?.judgeId;
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
      case OrderReviewStatus.AwaitingDocumentation:
        this.snackBarStore.showSnackbar(
          'The order review is awaiting documentation.',
          'rgb(46, 139, 43)',
          '⏳ Pending'
        );
        break;
    }
  }

  setToolbarItems(items: ToolbarItem[]): ToolbarItem[] {
    const toRemove = new Set(['note', 'print', 'callout', 'image']);
    const toMove = new Set(['open-information', 'open-document-review']);
    const base = items.filter(
      (item) =>
        !toRemove.has(item.type) && (item.id ? !toMove.has(item.id) : true)
    );

    // Icons rendered after the linearized-download-indicator or at the end
    const extras = [
      { type: 'spacer' },
      items.find((item) => item.id === 'open-information'),
      items.find((item) => item.type === 'image'),
      items.find((item) => item.id === 'open-document-review'),
    ].filter(Boolean) as ToolbarItem[];

    const anchor = base.findIndex(
      (item) => item.type === 'linearized-download-indicator'
    );
    const insertAt = anchor === -1 ? base.length : anchor + 1;

    return [...base.slice(0, insertAt), ...extras, ...base.slice(insertAt)];
  }
}
