import { OrderService } from '@/services';
import { useCommonStore, useSnackbarStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { OrderReview } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { ToolbarItem } from '@nutrient-sdk/viewer';
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
          'success',
          '✅ Approved!'
        );
        break;
      case OrderReviewStatus.Unapproved:
        this.snackBarStore.showSnackbar(
          'The order has been rejected.',
          'success',
          '📋 Rejected'
        );
        break;
      case OrderReviewStatus.AwaitingDocumentation:
        this.snackBarStore.showSnackbar(
          'The order review is awaiting documentation.',
          'success',
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
