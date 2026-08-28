import { OrderService, UserService } from '@/services';
import { useCommonStore, useSnackbarStore } from '@/stores';
import { StoreDocument } from '@/stores/PDFViewerStore';
import { Order, OrderReview } from '@/types';
import { OrderReviewStatus } from '@/types/common';
import { viewOrderSupportingDocuments } from '@/utils/orderDetails';
import {
  mdiAlphaIBoxOutline,
  mdiFileDocumentMultipleOutline,
  mdiFountainPenTip,
  mdiNotebookOutline,
} from '@mdi/js';
import { ToolbarItem } from '@nutrient-sdk/viewer';
import { inject, watch, WatchStopHandle } from 'vue';
import { FilePDFStrategy } from './FilePDFStrategy';
import { PDFViewerToolbarContext } from './PDFViewerTypes';

export class OrderPDFStrategy extends FilePDFStrategy {
  private readonly snackBarStore = useSnackbarStore();
  private readonly commonStore = useCommonStore();
  private readonly orderService: OrderService;
  private readonly userService: UserService;
  private readonly orderId: string | null;
  private readonly isShowingSupportingDocuments: boolean = false;
  private readonly hasSignature: boolean = false;
  private readonly hasInitials: boolean = false;
  private readonly showOrderReviewOptions: boolean = false;
  private currentOrder: Order | null = null;
  private judgeId: number | null = null;
  private readonly stopJudgeIdWatch: WatchStopHandle;

  private static readonly DEFAULT_SIGN_IMAGE_WIDTH = 210;
  private static readonly DEFAULT_SIGN_IMAGE_HEIGHT = 95;
  private static readonly DEFAULT_INITIALS_IMAGE_WIDTH = 55;
  private static readonly DEFAULT_INITIALS_IMAGE_HEIGHT = 30;
  private static readonly SIGNATURE_DESCRIPTION = 'Signature';
  private static readonly INITIALS_DESCRIPTION = 'Initials';

  // Custom Toolbar Item IDs
  private static readonly ID_OPEN_INFORMATION = 'open-information';
  private static readonly ID_OPEN_DOCUMENT_REVIEW = 'open-document-review';
  private static readonly ID_OPEN_SUPPORTING_DOCS = 'open-supporting-documents';
  private static readonly ID_ADD_SIGNATURE = 'add-signature';
  private static readonly ID_ADD_INITIALS = 'add-initials';

  constructor() {
    super();

    const orderService = inject<OrderService>('orderService');
    const userService = inject<UserService>('userService');

    if (!orderService || !userService) {
      throw new Error('Service(s) is undefined.');
    }

    this.orderService = orderService;
    this.userService = userService;
    this.showOrderReviewOptions =
      this.commonStore.userInfo?.judgeId != null &&
      this.commonStore.userInfo?.judgeId ===
        this.commonStore.loggedInUserInfo?.judgeId;
    this.judgeId = this.commonStore.userInfo?.judgeId ?? null;
    this.hasSignature = this.commonStore.userInfo?.hasSignature ?? false;
    this.hasInitials = this.commonStore.userInfo?.hasInitials ?? false;

    const urlParams = new URLSearchParams(globalThis.location.search);
    this.orderId = urlParams.get('id');
    if (!this.orderId) {
      throw new Error('Order ID is not defined in the URL parameters.');
    }

    this.isShowingSupportingDocuments =
      urlParams.get('isShowingSupportingDocs') === 'true';

    this.stopJudgeIdWatch = watch(
      () => this.commonStore.userInfo?.judgeId,
      (newJudgeId) => {
        this.judgeId = newJudgeId ?? null;
      }
    );
  }

  override cleanup(sessionId?: string): void {
    this.stopJudgeIdWatch();
    super.cleanup(sessionId);
  }

  protected override getOutlineDocumentTitle(document: StoreDocument): string {
    return document.documentName || 'Order';
  }

  // Retrieves the required "approval" annotations (signature and/or initials) based on its availability.
  getRequiredApprovalAnnotations(): string[] | undefined {
    const descriptions: string[] = [];
    if (this.hasSignature) {
      descriptions.push(OrderPDFStrategy.SIGNATURE_DESCRIPTION);
    }
    if (this.hasInitials) {
      descriptions.push(OrderPDFStrategy.INITIALS_DESCRIPTION);
    }
    return descriptions.length > 0 ? descriptions : undefined;
  }

  async reviewOrder(review: OrderReview): Promise<void> {
    if (!this.orderId) {
      throw new Error(`Order ID is not defined.`);
    }

    await this.orderService.review(this.orderId, review);

    switch (review.status) {
      case OrderReviewStatus.Approved:
      case OrderReviewStatus.OrderMade:
        this.snackBarStore.showSnackbar(
          'The order has been submitted.',
          'success',
          '✅ Submitted!'
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

  setToolbarItems(
    items: ToolbarItem[],
    context: PDFViewerToolbarContext
  ): ToolbarItem[] {
    const allItems = [...items, ...this.addCustomToolbarItems(context)];
    const toRemove = new Set([
      'note',
      'print',
      'callout',
      'image',
      'stamp',
      'link',
    ]);

    // Custom item ids we relocate
    const toMove = new Set([
      OrderPDFStrategy.ID_OPEN_SUPPORTING_DOCS,
      OrderPDFStrategy.ID_OPEN_INFORMATION,
      OrderPDFStrategy.ID_OPEN_DOCUMENT_REVIEW,
      OrderPDFStrategy.ID_ADD_SIGNATURE,
      OrderPDFStrategy.ID_ADD_INITIALS,
    ]);

    const base = allItems.filter(
      (item) =>
        !toRemove.has(item.type) && (item.id ? !toMove.has(item.id) : true)
    );

    const byId = new Map(
      allItems.filter((item) => item.id).map((item) => [item.id, item])
    );
    const imageItem = allItems.find((item) => item.type === 'image');

    const extras = this.isShowingSupportingDocuments
      ? []
      : ([
          { type: 'spacer' },
          byId.get(OrderPDFStrategy.ID_OPEN_SUPPORTING_DOCS),
          byId.get(OrderPDFStrategy.ID_OPEN_INFORMATION),
          // Keep the built-in image tool only when there's no signature/initials.
          !this.hasSignature && !this.hasInitials ? imageItem : null,
          byId.get(OrderPDFStrategy.ID_ADD_SIGNATURE),
          byId.get(OrderPDFStrategy.ID_ADD_INITIALS),
          byId.get(OrderPDFStrategy.ID_OPEN_DOCUMENT_REVIEW),
        ].filter(Boolean) as ToolbarItem[]);

    const anchor = base.findIndex(
      (item) => item.type === 'linearized-download-indicator'
    );
    const insertAt = anchor === -1 ? base.length : anchor + 1;

    return [...base.slice(0, insertAt), ...extras, ...base.slice(insertAt)];
  }

  addCustomToolbarItems(context: PDFViewerToolbarContext): ToolbarItem[] {
    const additionalToolbarItems: ToolbarItem[] = [];

    additionalToolbarItems.push({
      type: 'custom',
      id: OrderPDFStrategy.ID_OPEN_INFORMATION,
      title: 'Case details',
      icon: `<svg><path d="${mdiNotebookOutline}"/></svg>`,
      onPress: () => {
        const informationContext = context.resolveInformationContext(
          context.rawData
        );

        if (!informationContext) {
          console.warn('Unable to resolve PDF viewer information context.');
          return;
        }

        window.open(
          `${
            informationContext.isCriminal ? 'criminal-file/' : 'civil-file/'
          }${informationContext.physicalFileId}`,
          'relatedCaseInfo'
        );
      },
    });

    if (this.currentOrder?.hasSupportingDocs) {
      additionalToolbarItems.push({
        type: 'custom',
        id: OrderPDFStrategy.ID_OPEN_SUPPORTING_DOCS,
        title: 'View Supporting Documents',
        icon: `<svg><path d="${mdiFileDocumentMultipleOutline}"/></svg>`,
        onPress: () => viewOrderSupportingDocuments(this.currentOrder!),
      });
    }

    // Current user is not the judge assigned to this order
    // so don't show the order review options.
    if (!this.showOrderReviewOptions) {
      return additionalToolbarItems;
    }

    additionalToolbarItems.push({
      type: 'custom',
      id: OrderPDFStrategy.ID_OPEN_DOCUMENT_REVIEW,
      title: 'Submit',
      node: OrderPDFStrategy.createSubmitLabelNode(),
      className: 'jasper-submit-btn',
      onPress: () => {
        context.openReviewModal();
      },
    });

    if (this.hasSignature) {
      additionalToolbarItems.push(
        this.createImageToolbarItem(context, {
          id: OrderPDFStrategy.ID_ADD_SIGNATURE,
          title: 'Add Signature',
          iconPath: mdiFountainPenTip,
          fetchImage: () => this.userService.getSignature(),
          description: OrderPDFStrategy.SIGNATURE_DESCRIPTION,
          width: OrderPDFStrategy.DEFAULT_SIGN_IMAGE_WIDTH,
          height: OrderPDFStrategy.DEFAULT_SIGN_IMAGE_HEIGHT,
        })
      );
    }

    if (this.hasInitials) {
      additionalToolbarItems.push(
        this.createImageToolbarItem(context, {
          id: OrderPDFStrategy.ID_ADD_INITIALS,
          title: 'Add Initials',
          iconPath: mdiAlphaIBoxOutline,
          fetchImage: () => this.userService.getInitials(),
          description: OrderPDFStrategy.INITIALS_DESCRIPTION,
          width: OrderPDFStrategy.DEFAULT_INITIALS_IMAGE_WIDTH,
          height: OrderPDFStrategy.DEFAULT_INITIALS_IMAGE_HEIGHT,
        })
      );
    }

    return additionalToolbarItems;
  }

  // Custom items render content from `node`/`icon` only; `title` is just the tooltip.
  private static createSubmitLabelNode(): HTMLElement {
    const label = document.createElement('span');
    label.className = 'jasper-submit-btn__label';
    label.textContent = 'Submit';
    return label;
  }

  async initialize(): Promise<void> {
    const order = await this.orderService.getOrder(this.orderId!, this.judgeId);
    if (!order) {
      throw new Error(`Order with ID ${this.orderId} not found.`);
    }
    this.currentOrder = order;
  }

  private createImageToolbarItem(
    context: PDFViewerToolbarContext,
    config: {
      id: string;
      title: string;
      iconPath: string;
      fetchImage: () => Promise<Blob>;
      description: string;
      width: number;
      height: number;
    }
  ): ToolbarItem {
    return {
      type: 'custom',
      id: config.id,
      title: config.title,
      icon: `<svg><path d="${config.iconPath}"/></svg>`,
      onPress: () =>
        this.addImageToPage(
          context,
          config.fetchImage,
          config.description,
          config.width,
          config.height
        ),
    };
  }

  private async addImageToPage(
    context: PDFViewerToolbarContext,
    fetchImage: () => Promise<Blob>,
    description: string,
    imageWidth: number,
    imageHeight: number
  ): Promise<void> {
    try {
      const { instance, nutrientViewer } = context;
      const pageIndex = instance.viewState.currentPageIndex;

      const blob = await fetchImage();
      const attachmentId = await instance.createAttachment(blob);

      const pageInfo = instance.pageInfoForIndex(pageIndex);
      if (!pageInfo) {
        throw new Error(`Unable to resolve page info for page ${pageIndex}.`);
      }
      const { width, height } = pageInfo;

      const { width: fitWidth, height: fitHeight } =
        await OrderPDFStrategy.fitToBox(blob, imageWidth, imageHeight);

      const annotation = new nutrientViewer.Annotations.ImageAnnotation({
        pageIndex,
        contentType: blob.type,
        imageAttachmentId: attachmentId,
        description,
        boundingBox: new nutrientViewer.Geometry.Rect({
          left: (width - fitWidth) / 2,
          top: (height - fitHeight) / 2,
          width: fitWidth,
          height: fitHeight,
        }),
      });

      const newAnnotations = await instance.create(annotation);

      // Automatically select the newly added signature/initials
      const created = newAnnotations[0];
      if (created instanceof nutrientViewer.Annotations.ImageAnnotation) {
        instance.setSelectedAnnotations(
          nutrientViewer.Immutable.List([created.id])
        );
      }
    } catch (error) {
      const errorMessage = `Failed to add ${description.toLowerCase()}`;
      this.snackBarStore.showSnackbar(
        `${errorMessage}. Please try again.`,
        'error',
        '❌ Error'
      );
      console.error(`${errorMessage}:`, error);
    }
  }

  private static async fitToBox(
    blob: Blob,
    maxWidth: number,
    maxHeight: number
  ): Promise<{ width: number; height: number }> {
    const bitmap = await createImageBitmap(blob);
    try {
      const { width: naturalWidth, height: naturalHeight } = bitmap;
      if (!naturalWidth || !naturalHeight) {
        return { width: maxWidth, height: maxHeight };
      }
      const scale = Math.min(
        1,
        maxWidth / naturalWidth,
        maxHeight / naturalHeight
      );
      return {
        width: naturalWidth * scale,
        height: naturalHeight * scale,
      };
    } finally {
      bitmap.close();
    }
  }
}
