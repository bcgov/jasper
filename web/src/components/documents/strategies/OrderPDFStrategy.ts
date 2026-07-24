import { OrderService } from '@/services';
import { useCommonStore, useOrdersStore, useSnackbarStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { Order, OrderReview } from '@/types';
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
  private readonly ordersStore = useOrdersStore();
  private readonly orderService: OrderService;
  private readonly currentOrder: Order | undefined;
  private readonly isShowingSupportingDocuments: boolean;
  private readonly hasSupportingDocuments: boolean;

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
    const orderId = urlParams.get('id');
    this.isShowingSupportingDocuments =
      urlParams.get('isShowingSupportingDocs') === 'true';

    this.currentOrder = this.ordersStore.orders.find((o) => o.id === orderId);
    if (!this.currentOrder) {
      throw new Error('Current order not found in store.');
    }
    this.hasSupportingDocuments =
      [
        ...this.currentOrder.packageDocuments.filter(
          (pd) => !pd.referredDocument
        ),
        ...this.currentOrder.relevantCeisDocuments,
      ].length > 0;
  }

  protected override getOutlineDocumentTitle(document: StoreDocument): string {
    return document.documentName || 'Order';
  }

  async reviewOrder(review: OrderReview): Promise<void> {
    await this.orderService.review(this.currentOrder!.id, review);

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
        onPress: () => viewOrderSupportingDocuments(this.currentOrder!),
      },
    ];
  }
}
