import { OrderService } from '@/services';
import { useCommonStore, useSnackbarStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { OrderReview } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { viewOrderSupportingDocuments } from '@/utils/orderDetails';
import { mdiFileDocumentMultipleOutline } from '@mdi/js';
import { ToolbarItem } from '@nutrient-sdk/viewer';
import { inject } from 'vue';
import { FilePDFStrategy } from './FilePDFStrategy';

export class OrderPDFStrategy extends FilePDFStrategy {
  showOrderReviewOptions = true;

  private readonly snackBarStore = useSnackbarStore();
  private readonly commonStore = useCommonStore();
  private readonly orderService: OrderService;
  private readonly orderId: string | null;
  private readonly isShowingSupportingDocuments: boolean = false;
  private readonly hasSupportingDocuments: boolean = false;

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

    const urlParams = new URLSearchParams(globalThis.location.search);
    this.orderId = urlParams.get('id');
    if (!this.orderId) {
      throw new Error('Order ID is not defined in the URL parameters.');
    }

    this.isShowingSupportingDocuments =
      urlParams.get('isShowingSupportingDocs') === 'true';
    this.hasSupportingDocuments = urlParams.get('hasSupportingDocs') === 'true';
  }

  protected override getOutlineDocumentTitle(document: StoreDocument): string {
    return document.documentName || 'Order';
  }

  async reviewOrder(review: OrderReview): Promise<void> {
    if (!this.orderId) {
      throw new Error(`Order ID is not defined.`);
    }

    await this.orderService.review(this.orderId, review);

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
    const allItems = [...items, ...this.additionalToolbarItems()];
    const toRemove = new Set(['note', 'print', 'callout', 'image']);
    const toMove = new Set([
      'open-supporting-documents',
      'open-information',
      'open-document-review',
    ]);
    const base = allItems.filter(
      (item) =>
        !toRemove.has(item.type) && (item.id ? !toMove.has(item.id) : true)
    );

    const extras = !this.isShowingSupportingDocuments
      ? ([
          { type: 'spacer' },
          allItems.find((item) => item.id === 'open-supporting-documents'),
          allItems.find((item) => item.id === 'open-information'),
          allItems.find((item) => item.type === 'image'),
          allItems.find((item) => item.id === 'open-document-review'),
        ].filter(Boolean) as ToolbarItem[])
      : [];

    const anchor = base.findIndex(
      (item) => item.type === 'linearized-download-indicator'
    );
    const insertAt = anchor === -1 ? base.length : anchor + 1;

    return [...base.slice(0, insertAt), ...extras, ...base.slice(insertAt)];
  }

  additionalToolbarItems(): ToolbarItem[] {
    if (!this.hasSupportingDocuments) {
      return [];
    }

    return [
      {
        type: 'custom',
        id: 'open-supporting-documents',
        title: 'View Supporting Documents',
        icon: `<svg><path d="${mdiFileDocumentMultipleOutline}"/></svg>`,
        onPress: this.viewSupportingDocs.bind(this),
      },
    ];
  }

  async viewSupportingDocs(): Promise<void> {
    if (!this.orderId) {
      console.warn('No order id found. Cannot view supporting documents.');
      return;
    }

    const order = await this.orderService.getOrder(this.orderId);
    if (!order) {
      throw new Error(`Order with ID ${this.orderId} not found.`);
    }

    await viewOrderSupportingDocuments(order);
  }
}
